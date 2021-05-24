using CalendarSogo.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CalendarSogo.Controllers
{
    //[Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        public static readonly XNamespace xDav = XNamespace.Get("DAV:");
        public static readonly XNamespace xCalDav = XNamespace.Get("urn:ietf:params:xml:ns:caldav");
        static string[] Scopes = { CalendarService.Scope.CalendarReadonly };
        static string ApplicationName = "calendar4";
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            string calendarName;
            List<Ical.Net.Calendar> calendars = new List<Ical.Net.Calendar>();
            XDocument xProps = new XDocument(new XElement(xDav.GetName("propfind"), new XElement(xDav.GetName("allprop"))));
            XElement xElement = xProps.Root;
            string content = xElement.ToString();
            string readAll;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri("https://mx.sailau09.kz/SOGo/dav/test1@sailau09.kz/Calendar/"));
                request.Credentials = new NetworkCredential("postmaster@sailau09.kz", "!QAZ3edc");
                request.ContentType = "text/xml";
                ((HttpWebRequest)request).UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2";
                request.Headers.Add("Depth", "1");
                request.Method = "PROPFIND";
                request.ServerCertificateValidationCallback = delegate { return true; }; // при публикации на боевой сервер убрать

                using (var stream = request.GetRequestStream())
                {
                    var strWrt = new StreamWriter(stream);
                    strWrt.Write(content);
                }


                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        readAll = reader.ReadToEnd();
                        var xDocument = XDocument.Parse(readAll);
                        var statusCode = response.StatusCode;
                        var responses = xDocument.Descendants(xDav.GetName("response"));
                       
                        foreach (XElement res in responses)
                        {
                            if (res.Descendants(xCalDav.GetName("calendar")).Count() > 0)
                            {
                                string hrefRelative = res.Descendants(xDav.GetName("href")).First().Value;
                                
                                string href = "https://mx.sailau09.kz" + hrefRelative.TrimEnd('/') + ".ics";
                                var calendar= GetCalendarAsync(href).GetAwaiter().GetResult();
                                // var desc = res.Descendants(xCalDav.GetName("calendar-description")).FirstOrDefault();
                                // var name = res.Descendants(xDav.GetName("displayname")).FirstOrDefault();
                                calendars.Add(calendar);
                            }
                        }
                    }
                }
                response.Close();


            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            //получу список всех мероприятий из конкретного календаря
            // var events = calendar?.Events;

            //foreach (var item in calendar.Properties)
            //{
            //    if (item.Name == "X-WR-CALNAME")
            //    {
            //        calendarName = item.Value?.ToString();
            //    }

            //}

            foreach (var calendar in calendars)
            {
                foreach (var item in calendar.Properties)
                {
                    if (item.Name == "X-WR-CALNAME")
                    {
                        calendarName = item.Value?.ToString();
                    }
                }
               

            }

            return View(calendars);
            //return View();

        }

        public IActionResult Create()
        {
            var now = DateTime.Now.AddHours(1);
            var later = now.AddHours(2);
            CalendarSogo.Models.Event ev = new CalendarSogo.Models.Event()
            {
                Start = now,
                End = later,
            };
            return View(ev);
        }
        
        [HttpPost]
        public async Task< IActionResult> Create(CalendarSogo.Models.Event ev)
        {
            try
            {
                CalendarEvent e = new CalendarEvent()
                {
                    Description = ev.Description,
                    Summary = ev.Summary,
                    Start = new CalDateTime(ev.Start),
                    End = new CalDateTime(ev.End),
                    Location = "301 cabinet",
                    Comments=new List<string>() {"comment1" },
                    Contacts = new List<string>() { "Contacts" },
                    GeographicLocation = new GeographicLocation(),
                    
                    Organizer = new Organizer()
                    {
                        CommonName = "Organizer name",
                        Value = new Uri("mailto:org@sailau09.kz")

                    },
                    Attendees = new List<Attendee>()
                    {
                        new Attendee(new Uri("mailto:test2@sailau09.kz"))
                        {
                            CommonName ="test1 name",
                            Role=ParticipationRole.RequiredParticipant
                        },
                         new Attendee(new Uri("mailto:test3@sailau09.kz"))
                        {
                            CommonName ="test2 name",
                            Role=ParticipationRole.RequiredParticipant
                        },

                    }
                };
               
                
                string readAll;
                //HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://mx.sailau09.kz/SOGo/dav/test1@sailau09.kz/Calendar/personal/" + e.Uid + ".ics");
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://mx.sailau09.kz/SOGo/dav/test1@sailau09.kz/Calendar/dddd" + e.Uid + ".ics");
                request.Credentials = new NetworkCredential("postmaster@sailau09.kz", "!QAZ3edc");
                request.ContentType = "text/calendar";
                request.Headers.Add("If-None-Match", "*");
                request.Method = "PUT";
                request.ServerCertificateValidationCallback = delegate { return true; };// при публикации на боевой сервер убрать

                Ical.Net.Calendar calendar = new Ical.Net.Calendar();
                calendar.Events.Add(e);
                CalendarSerializer calendarSerializer = new CalendarSerializer();
                // возьмем поток и запишем туда наше мероприятие
                using (var stream = request.GetRequestStream())
                {
                    calendarSerializer.Serialize(calendar, stream, Encoding.UTF8);
                }
                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        var statusCode = response.StatusCode;
                        readAll = reader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            return RedirectToAction("Index");
        }
        public IActionResult CreateCalendar()
        {
            return View();
        }
        
       
        [HttpPost]
        public async Task<IActionResult> CreateCalendar(string calendarName)
        {
            string readAll;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri("https://mx.sailau09.kz/SOGo/dav/test1@sailau09.kz/Calendar/" + calendarName));
                request.Credentials = new NetworkCredential("postmaster@sailau09.kz", "!QAZ3edc");
                request.ContentType = "text/xml";
                request.Headers.Add("If-None-Match", "*");
                request.Method = "MKCALENDAR";
                request.ServerCertificateValidationCallback = delegate { return true; }; // при публикации на боевой сервер убрать
                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        readAll = reader.ReadToEnd();
                        var statusCode = response.StatusCode;
                    }
                }
                response.Close();


            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            return RedirectToAction("Index");
        }



        public async Task<IActionResult> GetCalendars()
        {
            XDocument xProps = new XDocument(new XElement(xDav.GetName("propfind"), new XElement(xDav.GetName("allprop"))));
            XElement xElement = xProps.Root;
            string content = xElement.ToString();
            string readAll;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri("https://mx.sailau09.kz/SOGo/dav/test1@sailau09.kz/Calendar/"));
                request.Credentials = new NetworkCredential("postmaster@sailau09.kz", "!QAZ3edc");
                request.ContentType = "text/xml";
                ((HttpWebRequest)request).UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2";
                request.Headers.Add("Depth", "1");
                request.Method = "PROPFIND";
                request.ServerCertificateValidationCallback = delegate { return true; }; // при публикации на боевой сервер убрать

                using (var stream = request.GetRequestStream())
                {
                   var strWrt = new StreamWriter(stream);
                    strWrt.Write(content);
                }
                

                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        readAll = reader.ReadToEnd();
                        var xDocument = XDocument.Parse(readAll);
                        var statusCode = response.StatusCode;
                        var responses = xDocument.Descendants(xDav.GetName("response"));
                        List<Ical.Net.Calendar> calendars = new List<Ical.Net.Calendar>();
                        foreach (XElement res in responses)
                        {
                            if (res.Descendants(xCalDav.GetName("calendar")).Count() > 0)
                            {
                                string href = res.Descendants(xDav.GetName("href")).First().Value;
                               // var desc = res.Descendants(xCalDav.GetName("calendar-description")).FirstOrDefault();
                               // var name = res.Descendants(xDav.GetName("displayname")).FirstOrDefault();
                                //calendars.Add(new Calendar(Common, new Uri(Url, href)) { Credentials = Credentials });
                            }
                        }
                    }
                }
                response.Close();


            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            return RedirectToAction("Index");
        }


        public async Task< IActionResult> Remove(string uid)
        {
            string readAll;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://mx.sailau09.kz/SOGo/dav/test1@sailau09.kz/Calendar/personal/"+ uid +".ics");
                request.Credentials = new NetworkCredential("postmaster@sailau09.kz", "!QAZ3edc");
                 request.ContentType = "text/calendar";
                //request.ContentType = "text/xml";
                request.Headers.Add("If-None-Match", "*");
                request.Method = "DELETE";
                request.ServerCertificateValidationCallback = delegate { return true; };// при публикации на боевой сервер убрать
                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        var statusCode=response.StatusCode;
                        readAll = reader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            return RedirectToAction("Index");
        }


       

        private async Task<Ical.Net.Calendar> GetCalendarAsync(string href)
        {
            Ical.Net.Calendar calendar = null;
            string calendarName;
            string readAll = "";
            try
            {
                // получу конкретный календарь personal
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(href);
                request.Credentials = new NetworkCredential("postmaster@sailau09.kz", "!QAZ3edc");
                request.ContentType = "text/xml";
                request.Headers.Add("Depth", "0");
                request.Headers.Add("If-None-Match", "*");
                request.Method = "GET";
                request.ServerCertificateValidationCallback = delegate { return true; }; // при публикации на боевой сервер убрать
                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        readAll = reader.ReadToEnd();
                        calendar = Ical.Net.Calendar.Load(readAll);
                    }
                }
                response.Close();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return calendar;
        }











        public IActionResult Privacy()
        {
            //var idToken = HttpContext.GetTokenAsync("id_token").GetAwaiter().GetResult();
            //var accessToken = HttpContext.GetTokenAsync("access_token").GetAwaiter().GetResult();

            UserCredential credential;

            using (var stream =
                new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Google Calendar API service.
            var service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                // ApiKey= "AIzaSyDdr1slmbKcsnR3_Ju6rGP6yQkHSqOPjBQ",
                ApplicationName = ApplicationName,
            });

            var gs = service.CalendarList.List();
            var fgs = gs.Execute();

            // Define parameters of request.
            EventsResource.ListRequest request = service.Events.List("primary");
            request.TimeMin = DateTime.Now;
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.MaxResults = 10;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            // List events.
            Events events = request.Execute();
            Console.WriteLine("Upcoming events:");
            if (events.Items != null && events.Items.Count > 0)
            {
                foreach (var eventItem in events.Items)
                {
                    string when = eventItem.Start.DateTime.ToString();
                    if (String.IsNullOrEmpty(when))
                    {
                        when = eventItem.Start.Date;
                    }
                    Console.WriteLine("{0} ({1})", eventItem.Summary, when);
                }
            }
            else
            {
                Console.WriteLine("No upcoming events found.");
            }
            Console.Read();


            return View();
        }



        public IActionResult RedirectUri()
        {
            return View();
        }
        
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
    public static class Common
    {
        public static bool Is(this string input, string other)
        {
            return string.Equals(input ?? string.Empty, other ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
    }
}
