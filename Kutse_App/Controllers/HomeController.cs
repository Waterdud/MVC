using Kutse_App.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Data.Entity;

namespace Kutse_App.Controllers
{
    public class HomeController : Controller
    {
        private GuestContext db = new GuestContext();

        private static readonly Dictionary<int, string> Pidu = new Dictionary<int, string>
        {
            {1, "Head uut aastat!"},
            {2, "Head Eesti iseseisvuspäeva!"},
            {12, "Haid jõule"}
        };

        public ActionResult Index()
        {
            string greeting;
            int month = DateTime.Now.Month;
            int hour = DateTime.Now.Hour;

            if (hour >= 6 && hour < 12)
                greeting = "Tere hommikust!";
            else if (hour >= 12 && hour < 18)
                greeting = "Tere päevast!";
            else if (hour >= 18 && hour < 21)
                greeting = "Tere õhtust!";
            else
                greeting = "Head ööd!";

            string holidayMessage = Pidu.ContainsKey(month) ? Pidu[month] : "";
            ViewBag.Greeting = greeting + (string.IsNullOrEmpty(holidayMessage) ? "" : " " + holidayMessage);
            ViewBag.Message = "Ootan sind minu peole! Palun tule!!!";

            return View();
        }

        [HttpGet]
        public ActionResult Ankeet()
        {
            var holidays = db.Holidays.ToList();
            ViewBag.Holidays = new SelectList(holidays, "Id", "Name");

            var guest = new Guest { WillAttend = User.IsInRole("Admin") };

            return View(guest);
        }

        [HttpPost]
        public ActionResult Ankeet(Guest guest)
        {
            if (!ModelState.IsValid)
            {
                var holidays = db.Holidays.ToList();
                ViewBag.Holidays = new SelectList(holidays, "Id", "Name");

                return View(guest);
            }

            try
            {
                db.Guests.Add(guest);
                db.SaveChanges();

                return RedirectToAction("Holidays");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Регистрация не удалась: " + ex.Message);
                return View(guest);
            }
        }

        public void E_mail(Guest guest)
        {
            try
            {
                WebMail.SmtpServer = "smtp.gmail.com";
                WebMail.SmtpPort = 587;
                WebMail.EnableSsl = true;
                WebMail.UserName = "glebsotjov@gmail.com";
                WebMail.Password = "fzyc svao svfj jqnp";
                WebMail.From = "glebsotjov@gmail.com";
                WebMail.Send(guest.Email, "Vastus kutsele", guest.Name + " vastas " + ((guest.WillAttend ?? false) ? " tuleb peole" : " ei tule saatnud"));
                ViewBag.Message = "Kiri on saatnud!";
            }
            catch (Exception)
            {
                ViewBag.Message = "Mul on kahju! Ei saa kirja saada!!!";
            }
        }

        [HttpPost]
        public ActionResult Meeldetuletus(Guest guest, string Meeldetuletus)
        {
            if (!string.IsNullOrEmpty(Meeldetuletus))
            {
                try
                {
                    WebMail.SmtpServer = "smtp.gmail.com";
                    WebMail.SmtpPort = 587;
                    WebMail.EnableSsl = true;
                    WebMail.UserName = "glebsotjov@gmail.com";
                    WebMail.Password = "fzyc svao svfj jqnp";
                    WebMail.From = "glebsotjov@gmail.com";

                    WebMail.Send(guest.Email, "Meeldetuletus", guest.Name + ", ära unusta. Pidu toimub 20.01.25! Sind ootavad väga!",
                        null, "glebsotjov@gmail.com",
                        filesToAttach: new String[] { Path.Combine(Server.MapPath("~/Images/"), Path.GetFileName("yippy.jpg")) }
                    );

                    ViewBag.Message = "Kutse saadetud!";
                }
                catch (Exception ex)
                {
                    ViewBag.Message = "Tekkis viga kutse saatmisel: " + ex.Message;
                }
            }

            return View("Aitäh", guest);
        }

        [HttpPost]
        public ActionResult SendReminder(string email)
        {
            var guest = db.Guests.Include(g => g.Holiday).FirstOrDefault(g => g.Email == email);

            if (guest == null)
            {
                ViewBag.Message = "Ühtegi registreeringut selle e-posti aadressiga ei leitud.";
                return View("MyRegistration", new List<Guest>());
            }

            try
            {
                WebMail.Send(guest.Email, "Meeldetuletus sündmusest", $"{guest.Name}, ärge unustage sündmust! See toimub {guest.Holiday?.Name ?? "määramata kuupäeval"}!");
                ViewBag.Message = "Meeldetuletus saadetud!";
            }
            catch (Exception)
            {
                ViewBag.Message = "Kahjuks ei saanud meeldetuletust saata!";
            }

            return View("MyRegistration", new List<Guest> { guest });
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Guests()
        {
            var guests = db.Guests.Include(g => g.Holiday).ToList();
            return View(guests);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public ActionResult Create()
        {
            var holidays = db.Holidays.ToList();
            ViewBag.Holidays = new SelectList(holidays, "Id", "Name");
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public ActionResult Create(Guest guest)
        {
            if (ModelState.IsValid)
            {
                db.Guests.Add(guest);
                db.SaveChanges();
                return RedirectToAction("Guests");
            }
            return View(guest);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public ActionResult Delete(int id)
        {
            Guest g = db.Guests.Find(id);
            return g == null ? HttpNotFound() : (ActionResult)View(g);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            Guest g = db.Guests.Find(id);
            if (g != null)
            {
                db.Guests.Remove(g);
                db.SaveChanges();
            }
            return RedirectToAction("Guests");
        }

        [Authorize(Roles = "Admin")]
        public ActionResult WillAttendGuests()
        {
            var guests = db.Guests.Where(g => g.WillAttend == true).ToList();
            return View("Guests", guests);
        }

        public ActionResult Holidays()
        {
            var holidays = db.Holidays.ToList();
            return View(holidays);
        }

        [HttpGet]
        public ActionResult MyRegistration(string email)
        {
            var guest = db.Guests.Include(g => g.Holiday).FirstOrDefault(g => g.Email == email);
            return View(guest == null ? new List<Guest>() : new List<Guest> { guest });
        }
    }
}
