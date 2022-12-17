using System;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Timers;
using Newtonsoft.Json;
using Timer = System.Threading.Timer;

namespace CanvasLMSAPI
{
    class Program
    {
        private static string ACCESS_TOKEN;
        private static string USER_ID;
        private static string CANVAS_URL;
        private static string DISCORD_WEBHOOK_URL;
        static int sec = 0;
        private static TimeSpan t;
        
        static void Main(string[] args)
        {
            if(File.Exists("config.txt"))
            {
                Console.WriteLine("Found Config file");
                string[] configText = File.ReadAllLines("config.txt");
                if (configText[2].Contains("CHANGE-ME"))
                {
                    Console.WriteLine("Error loading config. Please change all the default values");
                    Console.ReadKey();
                    return;
                }
                else
                {
                    ACCESS_TOKEN = configText[0].Split("=")[1].Replace(" ", "");
                    USER_ID = configText[1].Split("=")[1].Replace(" ", "");
                    CANVAS_URL = configText[2].Split("=")[1].Replace(" ", "");
                    DISCORD_WEBHOOK_URL = configText[3].Split("=")[1].Replace(" ", "");
                    Console.WriteLine("Success reading config!");
                }
            }
            else
            {
                string defaultConfig = "access_token = YOUR-TOKEN\nuser_id = self\ncanvas_url = CHANGE-ME.instructure.com\ndiscord_webhook_url = CHANGE-ME";
                File.WriteAllText("config.txt",defaultConfig);
                Console.WriteLine("Created default config file please edit it...");
                return;
            }
           
            // Set up an HTTP client for making API requests
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "CanvasLMSAPI/1.0");
            HttpResponseMessage Courses = client.GetAsync($"https://{CANVAS_URL}/api/v1/courses?access_token={ACCESS_TOKEN}").Result;
            Courses.EnsureSuccessStatusCode();
            string coursesJson = Courses.Content.ReadAsStringAsync().Result;
            dynamic courses = JsonConvert.DeserializeObject(coursesJson);
            // Get the current grades for the user
            Console.WriteLine("Retrieving current grades...");
            HttpResponseMessage response = client.GetAsync($"https://{CANVAS_URL}/api/v1/users/{USER_ID}/enrollments?access_token={ACCESS_TOKEN}").Result;
            response.EnsureSuccessStatusCode();
            string gradesJson = response.Content.ReadAsStringAsync().Result;
            dynamic grades = JsonConvert.DeserializeObject(gradesJson);

            // Print the current grades to the console
            Console.WriteLine("Current grades:");
            foreach (dynamic course in grades)
            {
                foreach (dynamic course2 in courses)
                {
                    if (course.course_id == course2.id)
                    {
                        string letter = ""+course.grades.current_grade;
                        letter= letter.Equals("") ? letter = "N/A" : letter;
                        
                        Console.WriteLine($"{course2.name}\t\nCurrent: {course.grades.current_score}%\t   Final: {course.grades.final_score}%({letter})");
                        
                    }
                }
                
            }

            Console.WriteLine();

            // Check for updates every 5 minutes
            while (true)
            {
                Console.WriteLine("Checking for updates...");

                // Get the updated grades for the user
                response = client.GetAsync($"https://{CANVAS_URL}/api/v1/users/{USER_ID}/enrollments?access_token={ACCESS_TOKEN}").Result;
                response.EnsureSuccessStatusCode();
                string updatedGradesJson = response.Content.ReadAsStringAsync().Result;
                dynamic updatedGrades = JsonConvert.DeserializeObject(updatedGradesJson);

                // Compare the updated grades to the current grades
                bool gradesChanged = false;
                foreach (dynamic updatedCourse in updatedGrades)
                {
                    foreach (dynamic currentCourse in grades)
                    {
                        if (updatedCourse.id == currentCourse.id)
                        {
                            if (updatedCourse.grades.current_score != currentCourse.grades.current_score ||
                                updatedCourse.grades.final_score != currentCourse.grades.final_score ||
                                updatedCourse.grades.current_grade != currentCourse.grades.current_grade)
                            {
                                gradesChanged = true;
                                break;
                            }
                        }
                    }
                }

                // If the grades have changed, send a notification to Discord
                if (gradesChanged)
                {
                    Console.Clear();
                    Console.WriteLine("Grades have changed!");

                    // Build the message to send to Discord
                    string message = "Your grades have been updated:\n";
                    foreach (dynamic updatedCourse in updatedGrades)
                    {
                        foreach (dynamic currentCourse in grades)
                        {
                            if (updatedCourse.id == currentCourse.id)
                            {
                                foreach (dynamic course2 in courses)
                                {
                                    if (updatedCourse.course_id == course2.id)
                                    {
                                        string letter = ""+updatedCourse.grades.current_grade;
                                        letter= letter.Equals("") ? letter = "N/A" : letter;
                                        message +=
                                            $"{course2.name} \nCurrent: {updatedCourse.grades.current_score}% \nFinal: {updatedCourse.grades.final_score}%({letter})\n\n";
                                        
                                    }
                                }
                            }
                        
                    }
                    }
                    //add server runtime for extra info
                    message+="Run time: "+ string.Format("{0:D2}h:{1:D2}m:{2:D2}s:", t.Hours, t.Minutes, t.Seconds);
                    // Send the message to Discord using the webhook
                    sendDiscordWebhook(DISCORD_WEBHOOK_URL,"","Canvas Update",message);
                    Console.WriteLine("Sent notification to Discord.");
                }
                else
                {
                 
                    
                    Console.WriteLine("{0} ","No changes detected.");
                    
                }

                // Update the current grades and wait for the next check
                grades = updatedGrades;
                //setup timer to count seconds for us
                Timer _timer = new Timer(DisplayTimeEvent, null, 0, 1000);
                Thread.Sleep(300000); // 5 minutes in milliseconds
                
            }
        }
        public static void sendDiscordWebhook(string URL, string profilepic, string username, string message)
        {
            NameValueCollection discordValues = new NameValueCollection();
            discordValues.Add("username", username);
            discordValues.Add("avatar_url", profilepic);
            discordValues.Add("content", message);
            new WebClient().UploadValues(URL, discordValues);
        }
        public static void DisplayTimeEvent( object source )
        {
           
            sec++;
           t = TimeSpan.FromSeconds(sec);
            Console.Write( " \r{0} " ,"Run time: "+ string.Format("{0:D2}h:{1:D2}m:{2:D2}s:", 
                t.Hours, 
                t.Minutes, 
                t.Seconds));
        }
    }
    
}
