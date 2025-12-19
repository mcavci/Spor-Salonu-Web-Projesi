using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SporSalonuProjesi.Data;
using SporSalonuProjesi.Models;
using System.Collections.Generic;
using System.Linq;

namespace SporSalonuProjesi.Controllers
{
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // 1. ADMIN GİRİŞ EKRANI (GET)
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string kadi, string sifre)
        {
            // Veritabanında bu email ve bu şifreye sahip biri var mı
            var admin = _context.Adminler.FirstOrDefault(x => x.Email == kadi && x.Sifre == sifre);

            if (admin != null)
            {

                HttpContext.Session.SetString("AdminOturumu", kadi);
                return RedirectToAction("Index");
            }

            // Giriş Başarısız
            ViewBag.Hata = "Hatalı kullanıcı adı veya şifre!";
            return View();
        }

        //  (YÖNETİM PANELİ - ANA SAYFA)
        public IActionResult Index()
        {

            // 1. Güvenlik Kontrolü
            if (HttpContext.Session.GetString("AdminOturumu") == null) return RedirectToAction("Login");

            // 2. İstatistik Kartları (Sayılar) - Bunlar hafif olduğu için burada kalabilir
            ViewBag.ToplamRandevu = _context.Randevular
                                     .Where(x => x.Durum == "Onaylandı" && x.Tarih >= DateTime.Now)
                                     .Count();
            ViewBag.BekleyenRandevu = _context.Randevular.Where(x => x.Durum == "Onay Bekliyor").Count();
            ViewBag.ToplamEgitmen = _context.Egitmenler.Count();
            ViewBag.ToplamPaket = _context.Paketler.Count();
            ViewBag.ToplamUye = _context.Uyeler.Count();

            // 3. Tablo Verisi (Bekleyen Randevular)
            var bekleyenRandevular = _context.Randevular
                                    .Where(x => x.Durum == "Onay Bekliyor")
                                    .OrderByDescending(x => x.Tarih)
                                    .ToList();

            return View(bekleyenRandevular);
        }

        // 4. RANDEVU ONAYLA
        public async Task<IActionResult> RandevuOnayla(int id)
        {
            if (HttpContext.Session.GetString("AdminOturumu") == null) return RedirectToAction("Login");

            var randevu = await _context.Randevular.FindAsync(id);
            if (randevu == null) return NotFound();

            randevu.Durum = "Onaylandı";

            _context.Update(randevu);
            await _context.SaveChangesAsync();

            TempData["Mesaj"] = "Randevu başarıyla onaylandı!";
            return RedirectToAction(nameof(Index));
        }

        // 5. RANDEVU SİL / REDDET
        public async Task<IActionResult> RandevuSil(int id)
        {
            if (HttpContext.Session.GetString("AdminOturumu") == null) return RedirectToAction("Login");

            var randevu = await _context.Randevular.FindAsync(id);

            if (randevu != null)
            {
                _context.Randevular.Remove(randevu);
                await _context.SaveChangesAsync();
                TempData["Hata"] = "Randevu iptal edildi ve silindi.";
            }

            return RedirectToAction(nameof(Index));
        }

        // 6. TÜM RANDEVULARI GÖR
        public IActionResult TumRandevular()
        {
            if (HttpContext.Session.GetString("AdminOturumu") == null) return RedirectToAction("Login");

            var liste = _context.Randevular.OrderByDescending(x => x.Tarih).ToList();
            return View(liste);
        }
        // 7. ÇIKIŞ YAP (GÜNCELLENMİŞ HALİ)
        public IActionResult Logout()
        {
            // tüm oturumu temizle
            HttpContext.Session.Clear();
            if (Request.Cookies["AdminOturumu"] != null)
            {
                Response.Cookies.Delete("AdminOturumu");
            }


            return RedirectToAction("Login", "Admin");
        }
    }
}