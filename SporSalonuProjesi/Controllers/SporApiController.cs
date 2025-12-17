using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SporSalonuProjesi.Data;
using SporSalonuProjesi.Models;
using System.Globalization;

namespace SporSalonuProjesi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SporApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SporApiController(AppDbContext context)
        {
            _context = context;
        }

        // 1. PAKETLERİ GETİREN API
        // İstek: GET api/sporapi/paketler
        [HttpGet("paketler")]
        public async Task<IActionResult> GetPaketler()
        {
            var paketler = await _context.Paketler
                                         .OrderBy(p => p.Fiyat)
                                         .ToListAsync();
            return Ok(paketler);
        }

        // 2. DERS PROGRAMINI GETİREN API
        // İstek: GET api/sporapi/dersler
        [HttpGet("dersler")]
        public IActionResult GetDersler()
        {
            // DTO (Data Transfer Object) ile göndermek daha sağlıklıdır ama          
            var dersler = _context.Dersler
                                  .Include(x => x.Egitmen)
                                  .ToList();
            return Ok(dersler);
        }

        // 3. DOLULUK DURUMUNU GETİREN API 
        // İstek: GET api/sporapi/doluluk?year=2025&month=12
        [HttpGet("doluluk")]
        public IActionResult GetDolulukDurumu(int year, int month)
        {
            DateTime baslangic = new DateTime(year, month, 1);
            DateTime bitis = baslangic.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59);

            var hamVeriler = _context.Randevular
                                     .Select(r => r.Saat)
                                     .ToList();

            var gunlukSayimlar = hamVeriler
                .Select(tarihString => DateTime.Parse(tarihString))
                .Where(tarih => tarih >= baslangic && tarih <= bitis)
                .GroupBy(x => x.Day)
                .Select(g => new { Gun = g.Key, Sayi = g.Count() })
                .ToList();

            var sonucListesi = new List<object>();
            int oAydakiGunSayisi = DateTime.DaysInMonth(year, month);

            for (int i = 1; i <= oAydakiGunSayisi; i++)
            {
                var kayit = gunlukSayimlar.FirstOrDefault(x => x.Gun == i);
                int randevuSayisi = kayit != null ? kayit.Sayi : 0;

                string durum = "MÜSAİT";
                string renk = "bg-success";

                if (randevuSayisi >= 15) { durum = "DOLU"; renk = "bg-danger"; }
                else if (randevuSayisi >= 8) { durum = "YOĞUN"; renk = "bg-warning text-dark"; }

                sonucListesi.Add(new
                {
                    gun = i,
                    durum = durum,
                    renk = renk,
                    sayi = randevuSayisi
                });
            }

            return Ok(sonucListesi);
        }

        // 4. PAKET SATIN ALMA API'Sİ
        // İstek: POST api/sporapi/paket-sec
        // Not: API'de Session kullanılmaz, ID'leri parametre olarak göndeririz.
        [HttpPost("paket-sec")]
        public async Task<IActionResult> PaketSec(int uyeId, int paketId)
        {
            var uye = await _context.Uyeler.FindAsync(uyeId);
            var secilenPaket = await _context.Paketler.FindAsync(paketId);

            if (uye == null || secilenPaket == null)
                return NotFound("Üye veya paket bulunamadı.");

            uye.PaketId = secilenPaket.PaketId;
            uye.PaketBitisTarihi = DateTime.Now.AddMonths(secilenPaket.SureAy);

            if (secilenPaket.SinirsizMi) uye.KalanAiHakki = 9999;
            else uye.KalanAiHakki = secilenPaket.ToplamAiHakki;

            _context.Update(uye);
            await _context.SaveChangesAsync();

            return Ok(new { mesaj = $"{secilenPaket.PaketAdi} başarıyla tanımlandı." });
        }

        [HttpGet("admin-grafik-verileri")]
        public IActionResult GetAdminGrafikVerileri()
        {
            var bugun = DateTime.Now;

            // --- 1. AYLIK KAZANÇ GRAFİĞİ HESAPLAMASI ---
            var aylarListesi = new List<string>();
            var kazancListesi = new List<decimal>();

            // Son 6 ayın verisini çekmek için genel bir sorgu (Hepsini tek tek çekmekten iyidir)
            var hamVeri = _context.Uyeler
                .Include(u => u.Paket)
                .Where(u => u.KayitTarihi >= bugun.AddMonths(-6))
                .ToList();

            for (int i = 5; i >= 0; i--)
            {
                var islemTarihi = bugun.AddMonths(-i);
                string ayAdi = islemTarihi.ToString("MMMM", new CultureInfo("tr-TR"));
                aylarListesi.Add(ayAdi);

                var oAyinKazanci = hamVeri
                    .Where(x => x.KayitTarihi.Month == islemTarihi.Month &&
                                x.KayitTarihi.Year == islemTarihi.Year)
                    .Sum(x => x.Paket != null ? x.Paket.Fiyat : 0);

                kazancListesi.Add(oAyinKazanci);
            }

            // --- 2. HOCA TERCİH GRAFİĞİ HESAPLAMASI ---
            var hocaAnalizi = _context.Randevular
            .Where(r => r.Durum == "Onaylandı")  
            .GroupBy(r => r.EgitmenAdi)
            .Select(g => new { HocaAdi = g.Key, Sayi = g.Count() })
            .ToList();

            var hocaIsimleri = hocaAnalizi.Select(x => x.HocaAdi).ToList();
            var hocaSayilari = hocaAnalizi.Select(x => x.Sayi).ToList();


            // PAKET DAĞILIMI (Hangi paketten kaç tane var?) ---
            var paketAnalizi = _context.Uyeler
                .Include(u => u.Paket)
                .GroupBy(u => u.Paket.PaketAdi)
                .Select(g => new { PaketAdi = g.Key, Sayi = g.Count() })
                .ToList();

            var paketIsimleri = paketAnalizi.Select(x => x.PaketAdi).ToList();
            var paketSayilari = paketAnalizi.Select(x => x.Sayi).ToList();


            //  SAATLİK YOĞUNLUK (Hangi saatte kaç randevu var?) ---
            // Sadece onaylı randevuların saatlerine bakıyoruz
            var saatAnalizi = _context.Randevular
                .Where(r => r.Durum == "Onaylandı")
                .AsEnumerable() // Saat string ise bellekte işlem yapmak daha güvenli olabilir
                .GroupBy(r => r.Saat) // Örn: "14:00", "15:00" gibi gruplar
                .OrderBy(g => g.Key)  // Sabah saatlerinden akşama doğru sırala
                .Select(g => new { Saat = g.Key, Sayi = g.Count() })
                .ToList();

            var saatler = saatAnalizi.Select(x => x.Saat).ToList();
            var saatYogunluklari = saatAnalizi.Select(x => x.Sayi).ToList();


            // Return kısmına yenilerini de ekliyoruz
            return Ok(new
            {
                // Eskiler
                aylar = aylarListesi,
                kazancData = kazancListesi,
                hocaIsim = hocaIsimleri,
                hocaSayi = hocaSayilari,

                // Yeniler
                paketIsim = paketIsimleri,
                paketSayi = paketSayilari,
                saatler = saatler,
                yogunluk = saatYogunluklari
            });
        }
    }
}