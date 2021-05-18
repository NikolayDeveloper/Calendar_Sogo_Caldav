using CalendarSogo.Models;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
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
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CalendarSogo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        public static readonly XNamespace xDav = XNamespace.Get("DAV:");
        public static readonly XNamespace xCalDav = XNamespace.Get("urn:ietf:params:xml:ns:caldav");

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            Calendar calendar=null;
            string calendarName;
            string readAll = "";
            try
            {
                // получу конкретный календарь personal
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://mx.sailau09.kz/SOGo/dav/test1@sailau09.kz/Calendar/created2%20calendar.ics");
                //HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://mx.sailau09.kz/SOGo/dav/test1@sailau09.kz/Calendar/personal.ics");
                request.Credentials = new NetworkCredential("postmaster@sailau09.kz", "!QAZ3edc");
                request.ContentType = "text/xml";
                request.Headers.Add("If-None-Match", "*");
                request.Method = "GET";
                request.ServerCertificateValidationCallback = delegate { return true; }; // при публикации на боевой сервер убрать
                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        readAll = reader.ReadToEnd();
                        calendar = Calendar.Load(readAll);
                    }
                }
                response.Close();
            }
            catch (Exception ex)
            {
                return BadRequest(new {message = ex.Message });
            }
            //получу список всех мероприятий из конкретного календаря
            var events = calendar?.Events;
            
            foreach (var item in calendar.Properties)
            {
                if(item.Name== "X-WR-CALNAME")
                {
                    calendarName = item.Value?.ToString();
                }
               
            }
            
            return View(events.ToList());

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
           
                Calendar calendar = new Calendar();
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
                        List<Calendar> calendars = new List<Calendar>();
                        // var hrefs = xdoc.Descendants(CalDav.Common.xDav.GetName("href"));
                        foreach (XElement res in responses)
                        {
                            if (res.Descendants(xCalDav.GetName("calendar")).Count() > 0)
                            {
                                string href = res.Descendants(xDav.GetName("href")).First().Value;
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


       

















        public IActionResult Privacy()
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
