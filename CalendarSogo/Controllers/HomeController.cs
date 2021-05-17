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

namespace CalendarSogo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            Calendar calendar=null;
            string readAll = "";
            try
            {
                // получу конкретный календарь personal
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://mx.sailau09.kz/SOGo/dav/test1@sailau09.kz/Calendar/personal.ics");
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
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://mx.sailau09.kz/SOGo/dav/test1@sailau09.kz/Calendar/personal/" + e.Uid + ".ics");
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
    
}
