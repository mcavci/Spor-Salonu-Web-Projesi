$(document).ready(function () {

    // Eğitmen veya Tarih değiştiğinde tetiklenir
    $("#drpEgitmen, #dtpTarih").change(function () {

        var egitmenId = $("#drpEgitmen").val();
        var tarih = $("#dtpTarih").val();
        var saatDropdown = $("#drpSaat");

        // HTML'den URL'yi dinamik olarak alıyoruz (Path hatasını çözer!)
        var urlAdresi = saatDropdown.data("url");

        if (egitmenId && tarih) {

            // Yükleniyor animasyonu ver
            saatDropdown.empty();
            saatDropdown.append('<option value="">Saatler Hesaplanıyor...</option>');
            saatDropdown.prop("disabled", true);

            $.ajax({
                url: urlAdresi, // URL artık HTML'den geliyor
                type: 'GET',
                data: { egitmenId: egitmenId, tarih: tarih },
                success: function (data) {
                    saatDropdown.empty();
                    saatDropdown.prop("disabled", false);

                    if (data.length > 0) {
                        saatDropdown.append('<option value="">-- Saat Seçiniz --</option>');
                        $.each(data, function (index, saat) {
                            saatDropdown.append('<option value="' + saat + '">' + saat + '</option>');
                        });
                    } else {
                        saatDropdown.append('<option value="">Bu tarihte boş saat yok!</option>');
                    }
                },
                error: function () {
                    saatDropdown.empty();
                    saatDropdown.append('<option value="">Bağlantı Hatası!</option>');
                    saatDropdown.prop("disabled", false);
                    // Hatayı konsola bas ki görelim
                    console.error("Ajax Hatası: URL'ye ulaşılamadı. URL: " + urlAdresi);
                }
            });
        } else {
            saatDropdown.empty();
            saatDropdown.append('<option value="">Önce Eğitmen ve Tarih Seçiniz</option>');
            saatDropdown.prop("disabled", true);
        }
    });
});