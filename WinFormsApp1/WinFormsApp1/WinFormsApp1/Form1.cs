using System;
using System.Data;
using System.Windows.Forms;
using Npgsql;
using Microsoft.Data.SqlClient;
using System.Reflection;
using System.Text.RegularExpressions;
using NpgsqlTypes;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        private Form2 form2;
        private Form3 form3;
        // PostgreSQL veritabaný baðlantý dizesi
        private string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=can12345;Database=sporSalonu";

        public Form1()
        {

            // Form2 ve Form3'ü baþlatýyoruz
            form2 = new Form2();
            form3 = new Form3();
            InitializeComponent();
            BilgileriGuncelleGenel();
        }


        //////////////////////////////////////////////////////////////////////////

        private void ClearControls(params Control[] controls)
        {
            foreach (var control in controls)
            {
                if (control is TextBox textBox) textBox.Clear();
                else if (control is ComboBox comboBox) comboBox.SelectedIndex = -1;
            }
        }

        private void BilgileriGuncelleGenel()
        {
            ListeleUyeler();
            ListelePaketler();
            PaketleriYukle();
            VerileriYukle();
            LoadPaketler();
            LoadOdemeData();
            AktifUyeleriGetir();
            LoadBilgilendirmeler();

        }

        private void GenelGuncelleme()
        {
            ClearControls(new Control[] { });
            BilgileriGuncelleGenel();
        }
        //////////////////////////////////////////////////////////////////////////



        // Üyeleri listelemek için
        // Üyeleri listelemek için
        private void ListeleUyeler()
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    // Uyeler ile Insan tablolarýný birleþtirerek gerekli bilgileri seçiyoruz.
                    string query = @"
SELECT 
    u.uye_id,
    i.tc_numarasi,
    i.ad,
    i.soyad,
    i.dogum_tarihi,  -- Doðum tarihi eklendi
    i.email,
    i.telefon,
    i.kayit_tarihi,
    u.durum,
    u.kalan_giris,
    -- Puanlar tablosundaki puan deðerini alýyoruz. Eðer yoksa 0 döndürülür.
    COALESCE(p.puan, 0) AS puan
FROM ""Uyeler"" u
INNER JOIN ""Insan"" i ON u.insan_id = i.insan_id
LEFT JOIN ""Puanlar"" p ON u.uye_id = p.uye_id";  // LEFT JOIN ile Puanlar tablosundan veri alýyoruz.

                    NpgsqlDataAdapter dataAdapter = new NpgsqlDataAdapter(query, connection);
                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);
                    dgvUyeler.DataSource = dataTable;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata oluþtu: {ex.Message}");
            }
        }








        // Üyeleri TC numarasýna göre aramak için
        // Üyeleri TC numarasýna göre aramak için
        private void AraUyeler()
        {
            if (string.IsNullOrWhiteSpace(txtTcAra.Text))
            {
                MessageBox.Show("Lütfen geçerli bir TC numarasý girin.", "Uyarý", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var connection = new NpgsqlConnection(connectionString))
            {
                try
                {
                    connection.Open(); // Baðlantýyý aç

                    // Insan ve Uyeler tablolarýný iliþkilendiren sorgu
                    string query = @"
SELECT 
    i.tc_numarasi, 
    i.ad, 
    i.soyad, 
    i.dogum_tarihi, 
    i.email, 
    i.telefon, 
    i.kayit_tarihi,
    u.uye_id, 
    u.durum, 
    u.kalan_giris,
    -- Puanlar tablosundan puan bilgisi çekiliyor.
    COALESCE(p.puan, 0) AS puan
FROM ""Insan"" i
INNER JOIN ""Uyeler"" u ON i.insan_id = u.insan_id
LEFT JOIN ""Puanlar"" p ON u.uye_id = p.uye_id  -- Puanlar tablosunu LEFT JOIN ile dahil ediyoruz
WHERE i.tc_numarasi = @tc_numarasi";  // Girilen TC numarasýna göre sorgulama yapýyoruz

                    // Sorguyu çalýþtýrmak için DataAdapter kullanýmý
                    NpgsqlDataAdapter dataAdapter = new NpgsqlDataAdapter(query, connection);
                    dataAdapter.SelectCommand.Parameters.AddWithValue("@tc_numarasi", txtTcAra.Text.Trim());

                    // Gelen verileri DataTable'a aktar
                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);

                    // Sonuçlarý kontrol et
                    if (dataTable.Rows.Count == 0)
                    {
                        MessageBox.Show("Girilen TC numarasýna ait bir üye bulunamadý. Tüm üyeler listeleniyor.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ListeleUyeler(); // Tüm üyeleri yeniden listele
                    }
                    else
                    {
                        dgvUyeler.DataSource = null; // Önceki referansý temizle
                        dgvUyeler.DataSource = dataTable; // Sonuçlarý DataGridView'de göster
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Bir hata oluþtu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }





        // TC numarasýna göre arama yapma
        private void btnAra_Click(object sender, EventArgs e)
        {
            AraUyeler(); // TC numarasýna göre arama yap
            txtTcAra.Clear();
        }

        // Paketleri ComboBox'a yükleme
        private void PaketleriYukle()
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                // Paketleri "Uyelik_Paketleri" tablosundan seçiyoruz
                string query = "SELECT paket_id, paket_adi FROM \"Uyelik_Paketleri\"";
                using (var cmd = new NpgsqlCommand(query, connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        // ComboBox'ý temizle
                        cbPaketSecimi.Items.Clear();

                        // Paketleri ComboBox'a ekle
                        while (reader.Read())
                        {
                            // Paket ismini ve ID'sini ekle
                            cbPaketSecimi.Items.Add(new
                            {
                                PaketAdi = reader.GetString(1),
                                PaketId = reader.GetInt32(0)
                            });
                        }
                    }
                }
            }

            // ComboBox DisplayMember ve ValueMember ayarlarýný yap
            cbPaketSecimi.DisplayMember = "PaketAdi";
            cbPaketSecimi.ValueMember = "PaketId";
        }

        // Paket seçimi deðiþtiðinde fiyat ve giriþ limitini güncelleyen event handler
        private void cbPaketSecimi_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbPaketSecimi.SelectedIndex != -1)
            {
                // Seçilen paketin ID'sini al
                int paketId = ((dynamic)cbPaketSecimi.SelectedItem).PaketId;

                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    // Seçilen paket için fiyat ve giriþ limitini al
                    string query = "SELECT fiyat, giris_limiti FROM \"Uyelik_Paketleri\" WHERE paket_id = @paket_id";
                    using (var cmd = new NpgsqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@paket_id", paketId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                decimal fiyat = reader.GetDecimal(0);
                                int girisLimit = reader.GetInt32(1);

                                // Fiyat ve giriþ limitini etiketlere yaz
                                //lblPaketFiyat.Text = $"Fiyat: {fiyat:C}";
                                //lblGirisLimit.Text = $"Giriþ Limiti: {girisLimit}";
                            }
                        }
                    }
                }
            }
            else
            {
                // Paket seçilmediyse etiketleri temizle
                //lblPaketFiyat.Text = "Fiyat: -";
                //lblGirisLimit.Text = "Giriþ Limiti: -";
            }
        }


        // Üye ekleme butonuna týklanýnca çalýþacak fonksiyon
        private void btnUyeEkle_Click(object sender, EventArgs e)
        {
            // Boþ alan kontrolü
            if (string.IsNullOrWhiteSpace(txtTc.Text) || string.IsNullOrWhiteSpace(txtAd.Text) ||
                string.IsNullOrWhiteSpace(txtSoyad.Text) || cbPaketSecimi.SelectedIndex == -1 ||
                string.IsNullOrWhiteSpace(txtEmail.Text) || string.IsNullOrWhiteSpace(txtTelefon.Text))
            {
                MessageBox.Show("Lütfen tüm alanlarý doldurun.");
                return;
            }

            // TC numarasýnýn 11 basamaktan oluþup oluþmadýðýný kontrol et
            if (txtTc.Text.Length != 11 || !txtTc.Text.All(char.IsDigit))
            {
                MessageBox.Show("TC numarasý 11 haneli ve sadece rakamlardan oluþmalýdýr.");
                return;
            }

            // Yaþ hesaplama
            int yas = DateTime.Now.Year - dtpDogumTarihi.Value.Year;
            if (dtpDogumTarihi.Value > DateTime.Now.AddYears(-yas)) yas--;

            // 18 yaþýndan küçük üyeleri eklememek için kontrol
            if (yas < 18)
            {
                MessageBox.Show("Üye yaþý 18'den küçük olamaz.");
                return;
            }

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                // Ayný TC numarasýyla kayýt olup olmadýðýný kontrol et
                string kontrolQuery = "SELECT COUNT(*) FROM \"Insan\" WHERE tc_numarasi = @tc_numarasi";
                using (var kontrolCmd = new NpgsqlCommand(kontrolQuery, connection))
                {
                    kontrolCmd.Parameters.AddWithValue("@tc_numarasi", txtTc.Text);
                    int insanSayisi = Convert.ToInt32(kontrolCmd.ExecuteScalar());
                    if (insanSayisi > 0)
                    {
                        MessageBox.Show("Bu TC numarasýyla zaten bir kayýt var.");
                        return;
                    }
                }

                // `Insan` tablosuna kayýt ekleme, doðum tarihini de ekliyoruz
                string insanQuery = "INSERT INTO \"Insan\" (ad, soyad, tc_numarasi, email, telefon, dogum_tarihi) " +
                                    "VALUES (@ad, @soyad, @tc_numarasi, @email, @telefon, @dogum_tarihi) RETURNING insan_id";
                int insanId;
                using (var insanCmd = new NpgsqlCommand(insanQuery, connection))
                {
                    insanCmd.Parameters.AddWithValue("@ad", txtAd.Text);
                    insanCmd.Parameters.AddWithValue("@soyad", txtSoyad.Text);
                    insanCmd.Parameters.AddWithValue("@tc_numarasi", txtTc.Text);
                    insanCmd.Parameters.AddWithValue("@email", txtEmail.Text);
                    insanCmd.Parameters.AddWithValue("@telefon", txtTelefon.Text);
                    insanCmd.Parameters.AddWithValue("@dogum_tarihi", dtpDogumTarihi.Value);  // Doðum tarihi parametresini ekledik
                    insanId = Convert.ToInt32(insanCmd.ExecuteScalar());
                }

                // Seçilen paket ID'sini al
                int paketId = ((dynamic)cbPaketSecimi.SelectedItem).PaketId;

                // `Uyeler` tablosuna kayýt ekleme
                string uyeQuery = "INSERT INTO \"Uyeler\" (insan_id, durum, kalan_giris) " +
                                  "VALUES (@insan_id, 'Aktif', " +
                                  "(SELECT giris_limiti FROM \"Uyelik_Paketleri\" WHERE paket_id = @paket_id))";
                using (var uyeCmd = new NpgsqlCommand(uyeQuery, connection))
                {
                    uyeCmd.Parameters.AddWithValue("@insan_id", insanId);
                    uyeCmd.Parameters.AddWithValue("@paket_id", paketId);
                    uyeCmd.ExecuteNonQuery();
                }

                MessageBox.Show("Üye baþarýyla eklendi.");
            }

            // Üyeleri listeleme
            ListeleUyeler();
            txtTc.Clear();
            txtAd.Clear();
            txtSoyad.Clear();
            txtEmail.Clear();
            txtTelefon.Clear();
            dtpDogumTarihi.Value = DateTime.Now;
            cbPaketSecimi.SelectedIndex = -1;
        }



        // Üye silme butonuna týklanýnca çalýþacak fonksiyon
        private void btnUyeSil_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTcSil.Text))
            {
                MessageBox.Show("Lütfen silmek istediðiniz üyenin TC numarasýný girin.", "Uyarý", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                // `tc_numarasi` üzerinden `insan_id`yi al
                string insanQuery = "SELECT insan_id FROM \"Insan\" WHERE tc_numarasi = @tc_numarasi";
                int insanId;
                using (var insanCmd = new NpgsqlCommand(insanQuery, connection))
                {
                    insanCmd.Parameters.AddWithValue("@tc_numarasi", txtTcSil.Text);
                    var result = insanCmd.ExecuteScalar();
                    if (result == null)
                    {
                        MessageBox.Show("Girilen TC numarasýna ait bir kayýt bulunamadý.");
                        return;
                    }
                    insanId = Convert.ToInt32(result);
                }

                // `Uyeler` tablosundan silme iþlemi
                string uyeDeleteQuery = "DELETE FROM \"Uyeler\" WHERE insan_id = @insan_id";
                using (var uyeDeleteCmd = new NpgsqlCommand(uyeDeleteQuery, connection))
                {
                    uyeDeleteCmd.Parameters.AddWithValue("@insan_id", insanId);
                    uyeDeleteCmd.ExecuteNonQuery();
                }

                // `Insan` tablosundan silme iþlemi
                string insanDeleteQuery = "DELETE FROM \"Insan\" WHERE insan_id = @insan_id";
                using (var insanDeleteCmd = new NpgsqlCommand(insanDeleteQuery, connection))
                {
                    insanDeleteCmd.Parameters.AddWithValue("@insan_id", insanId);
                    insanDeleteCmd.ExecuteNonQuery();
                }

                MessageBox.Show("Üye baþarýyla silindi.");
            }
            ListeleUyeler(); // Üyeleri yeniden listele
            txtTcSil.Clear();
        }




        // Giriþ yap butonuna týklanýnca çalýþacak fonksiyon
        private void btnGirisYap_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTcGiris.Text))
            {
                MessageBox.Show("Lütfen TC numarasýný girin.");
                return;
            }

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                // TC numarasý üzerinden üye bilgilerini al
                string query = "SELECT u.uye_id, u.durum, u.kalan_giris " +
                               "FROM \"Uyeler\" u " +
                               "JOIN \"Insan\" i ON u.insan_id = i.insan_id " +
                               "WHERE i.tc_numarasi = @tc_numarasi";
                using (var cmd = new NpgsqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@tc_numarasi", txtTcGiris.Text);
                    var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        int uyeId = reader.GetInt32(0); // Üyenin ID'si
                        string durum = reader.GetString(1); // Üyenin durumu
                        int kalanGiris = reader.GetInt32(2); // Kalan giriþ hakký

                        reader.Close(); // Reader'ý kapat

                        // Durum ve giriþ hakký kontrolü
                        if (durum == "Aktif")
                        {
                            MessageBox.Show("Üye zaten aktif, tekrar giriþ yapýlamaz.");
                            return;
                        }
                        else if (kalanGiris <= 0)
                        {
                            MessageBox.Show("Üyenin giriþ hakký kalmamýþ.");
                            return;
                        }

                        // Puan kontrolü ve ödül iþlemi
                        string puanQuery = "SELECT puan FROM \"Puanlar\" WHERE uye_id = @uye_id";
                        using (var puanCmd = new NpgsqlCommand(puanQuery, connection))
                        {
                            puanCmd.Parameters.AddWithValue("@uye_id", uyeId);
                            var puanResult = puanCmd.ExecuteScalar();
                            int puan = puanResult != null ? Convert.ToInt32(puanResult) : 0;

                            if (puan >= 500)
                            {
                                // 500 puan düþür ve 5 giriþ hakký ekle
                                string updatePuanQuery = "UPDATE \"Puanlar\" SET puan = puan - 500 WHERE uye_id = @uye_id";
                                string updateGirisQuery = "UPDATE \"Uyeler\" SET kalan_giris = kalan_giris + 5 WHERE uye_id = @uye_id";

                                using (var updatePuanCmd = new NpgsqlCommand(updatePuanQuery, connection))
                                using (var updateGirisCmd = new NpgsqlCommand(updateGirisQuery, connection))
                                {
                                    updatePuanCmd.Parameters.AddWithValue("@uye_id", uyeId);
                                    updateGirisCmd.Parameters.AddWithValue("@uye_id", uyeId);

                                    updatePuanCmd.ExecuteNonQuery();
                                    updateGirisCmd.ExecuteNonQuery();

                                    MessageBox.Show("Üyenin 500 puan hediye olarak 5 giriþ hakký ile deðiþtirildi.");
                                }
                            }
                        }

                        // Üyeyi aktif yap
                        string updateQuery = "UPDATE \"Uyeler\" SET durum = 'Aktif' WHERE uye_id = @uye_id";
                        using (var updateCmd = new NpgsqlCommand(updateQuery, connection))
                        {
                            updateCmd.Parameters.AddWithValue("@uye_id", uyeId);
                            updateCmd.ExecuteNonQuery();
                        }

                        // Aktif Üyeler tablosuna giriþ kaydýný ekle
                        string aktifUyeQuery = "INSERT INTO \"Aktif_Uyeler\" (uye_id, aktif_baslangic) VALUES (@uye_id, CURRENT_TIMESTAMP)";
                        using (var aktifCmd = new NpgsqlCommand(aktifUyeQuery, connection))
                        {
                            aktifCmd.Parameters.AddWithValue("@uye_id", uyeId);
                            aktifCmd.ExecuteNonQuery();
                        }

                        // Girilenler tablosuna giriþ kaydýný ekle
                        string girilenlerQuery = "INSERT INTO \"Girilenler\" (uye_id, giris_tarihi) VALUES (@uye_id, CURRENT_TIMESTAMP)";
                        using (var girilenlerCmd = new NpgsqlCommand(girilenlerQuery, connection))
                        {
                            girilenlerCmd.Parameters.AddWithValue("@uye_id", uyeId);
                            girilenlerCmd.ExecuteNonQuery();
                        }

                        // Aktif kiþi sayýsýný güncelle
                        AktifKisiSayisiGuncelle();

                        MessageBox.Show("Giriþ baþarýlý.");
                    }
                    else
                    {
                        MessageBox.Show("Üye bulunamadý.");
                    }
                }
            }
            ListeleUyeler();
            txtTcGiris.Clear();
        }







        // Çýkýþ yap butonuna týklanýnca çalýþacak fonksiyon
        private void btnCikisYap_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTcCikis.Text))
            {
                MessageBox.Show("Lütfen çýkýþ yapacak üyenin TC numarasýný girin.");
                return;
            }

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                // TC numarasý üzerinden üye bilgilerini al
                string query = "SELECT u.uye_id, u.durum " +
                               "FROM \"Uyeler\" u " +
                               "JOIN \"Insan\" i ON u.insan_id = i.insan_id " +
                               "WHERE i.tc_numarasi = @tc_numarasi";
                using (var cmd = new NpgsqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@tc_numarasi", txtTcCikis.Text);
                    var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        int uyeId = reader.GetInt32(0);  // Üyenin ID'si
                        string durum = reader.GetString(1);  // Üyenin durumu

                        // Durum kontrolü: Üye aktif mi?
                        if (durum == "Pasif")
                        {
                            MessageBox.Show("Üye zaten pasif, çýkýþ yapýlamaz.");
                        }
                        else
                        {
                            reader.Close();

                            // Üyeyi pasif yap
                            string updateQuery = "UPDATE \"Uyeler\" SET durum = 'Pasif' WHERE uye_id = @uye_id";
                            using (var updateCmd = new NpgsqlCommand(updateQuery, connection))
                            {
                                updateCmd.Parameters.AddWithValue("@uye_id", uyeId);
                                updateCmd.ExecuteNonQuery();
                            }

                            // Aktif Üyeler tablosunda çýkýþ saatini güncelle
                            string aktifUyeUpdateQuery = "UPDATE \"Aktif_Uyeler\" SET aktif_bitis = CURRENT_TIMESTAMP WHERE uye_id = @uye_id AND aktif_bitis IS NULL";
                            using (var aktifUpdateCmd = new NpgsqlCommand(aktifUyeUpdateQuery, connection))
                            {
                                aktifUpdateCmd.Parameters.AddWithValue("@uye_id", uyeId);
                                aktifUpdateCmd.ExecuteNonQuery();
                            }

                            // Çýkýþ tablosuna kayýt ekle
                            string cikisQuery = "INSERT INTO \"Cikis\" (uye_id, cikis_tarihi) VALUES (@uye_id, CURRENT_TIMESTAMP)";
                            using (var cikisCmd = new NpgsqlCommand(cikisQuery, connection))
                            {
                                cikisCmd.Parameters.AddWithValue("@uye_id", uyeId);
                                cikisCmd.ExecuteNonQuery();
                            }

                            // Aktif kiþi sayýsýný güncelle
                            AktifKisiSayisiGuncelle();

                            MessageBox.Show("Çýkýþ iþlemi baþarýlý.");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Üye bulunamadý.");
                    }
                    reader.Close();  // Reader kapatýlmalý
                }
            }
            ListeleUyeler(); // Üyeleri yeniden listele
            txtTcCikis.Clear();
        }










        // Paketlerin listelenmesi
        private void ListelePaketler()
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT paket_id, paket_adi, fiyat, giris_limiti FROM \"Uyelik_Paketleri\""; // 3 özellik eklenmiþtir
                using (var cmd = new NpgsqlCommand(query, connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        DataTable dt = new DataTable();
                        dt.Load(reader);
                        dgvPaketler.DataSource = dt; // Paketlerin listeleneceði DataGridView
                    }
                }
            }
        }

        // Paket ekleme butonuna týklanýnca çalýþacak fonksiyon
        private void btnPaketEkle_Click(object sender, EventArgs e)
        {
            // Boþ alan kontrolü
            if (string.IsNullOrWhiteSpace(txtPaketAdi.Text) || string.IsNullOrWhiteSpace(txtPaketFiyat.Text) || string.IsNullOrWhiteSpace(txtGirisLimiti.Text))
            {
                MessageBox.Show("Lütfen tüm alanlarý doldurun.");
                return;
            }

            // Fiyat ve giriþ limiti için tür kontrolü
            decimal fiyat;
            if (!decimal.TryParse(txtPaketFiyat.Text, out fiyat))
            {
                MessageBox.Show("Lütfen geçerli bir fiyat girin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int girisLimiti;
            if (!int.TryParse(txtGirisLimiti.Text, out girisLimiti))
            {
                MessageBox.Show("Lütfen geçerli bir giriþ limiti girin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string paketAdi = txtPaketAdi.Text;

            // Veritabanýna baðlanarak paket ekleme iþlemi
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                // Paket ekleme sorgusu
                string query = "INSERT INTO \"Uyelik_Paketleri\" (paket_adi, fiyat, giris_limiti) VALUES (@paket_adi, @fiyat, @giris_limiti)";
                using (var cmd = new NpgsqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@paket_adi", paketAdi);
                    cmd.Parameters.AddWithValue("@fiyat", fiyat);
                    cmd.Parameters.AddWithValue("@giris_limiti", girisLimiti);
                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Paket baþarýyla eklendi.");
                ListelePaketler(); // Paketler tekrar listelensin
            }

            // Yüklemeler ve temizlemeler
            PaketleriYukle();
            LoadPaketler();
            txtPaketAdi.Clear();
            txtPaketFiyat.Clear();
            txtGirisLimiti.Clear();
        }


        // Paket silme butonuna týklanýnca çalýþacak fonksiyon
        private void btnPaketSil_Click(object sender, EventArgs e)
        {
            // Paket ID'si boþ ise kullanýcýyý uyar
            if (string.IsNullOrWhiteSpace(txtPaketIdSil.Text))
            {
                MessageBox.Show("Lütfen silmek istediðiniz paketin ID'sini girin.", "Uyarý", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Paket ID'sini al
            int paketId;
            if (!int.TryParse(txtPaketIdSil.Text, out paketId))
            {
                MessageBox.Show("Lütfen geçerli bir paket ID'si girin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Veritabanýna baðlanarak SQL fonksiyonunu çaðýr
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                // SQL fonksiyonunu çaðýrma
                string query = "SELECT sil_paket(@paket_id)";
                using (var cmd = new NpgsqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@paket_id", paketId);

                    try
                    {
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Paket baþarýyla silindi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ListelePaketler(); // Paketler tekrar listelensin
                    }
                    catch (PostgresException ex)
                    {
                        // Hata mesajýný kullanýcýya göster
                        MessageBox.Show($"Paket silme sýrasýnda hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

            // Silme sonrasý temizleme
            txtPaketIdSil.Clear();
        }




        // Verileri yükleme fonksiyonu
        private void VerileriYukle()
        {
            try
            {
                // DataGridView'i temizle
                dgvSporAletleri.Rows.Clear();
                dgvSporAletleri.Columns.Clear(); // Mevcut sütunlarý temizle

                // Sütunlarý oluþtur
                dgvSporAletleri.Columns.Add("aletID", "Alet ID");
                dgvSporAletleri.Columns.Add("aletAdi", "Alet Adý");
                dgvSporAletleri.Columns.Add("aletTuru", "Alet Türü");
                dgvSporAletleri.Columns.Add("fiyat", "Fiyat");
                dgvSporAletleri.Columns.Add("miktar", "Miktar");

                string query = @"
            SELECT * FROM getir_spor_aletleri();";

                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                    {
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int aletID = reader.GetInt32(0);
                                string aletAdi = reader.GetString(1);
                                string aletTuru = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                                decimal fiyat = reader.GetDecimal(3);
                                int miktar = reader.GetInt32(4);

                                // DataGridView'e satýr ekle
                                dgvSporAletleri.Rows.Add(aletID, aletAdi, aletTuru, fiyat.ToString("C"), miktar);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Veri Yükleme Hatasý", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        // Spor aleti ekleme fonksiyonu
        private void btnAletEkle_Click(object sender, EventArgs e)
        {
            string aletAdi = txtAletAdi.Text;
            string aletTuru = txtAletTuru.Text;
            decimal fiyat;
            int miktar;

            // Fiyat ve Miktar doðrulamasý
            if (string.IsNullOrWhiteSpace(aletAdi) || string.IsNullOrWhiteSpace(aletTuru))
            {
                MessageBox.Show("Lütfen alet adý ve türünü giriniz.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtFiyat.Text, out fiyat) || fiyat <= 0)
            {
                MessageBox.Show("Geçerli bir fiyat giriniz.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(txtMiktar.Text, out miktar) || miktar < 0)
            {
                MessageBox.Show("Geçerli bir miktar giriniz.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Temel_Spor_Aleti'ne ekleme
            try
            {
                string queryTemel = "INSERT INTO \"Temel_Spor_Aleti\" (alet_adi, alet_turu, fiyat) VALUES (@alet_adi, @alet_turu, @fiyat) RETURNING alet_id;";

                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    using (NpgsqlCommand cmd = new NpgsqlCommand(queryTemel, conn))
                    {
                        cmd.Parameters.AddWithValue("@alet_adi", aletAdi);
                        cmd.Parameters.AddWithValue("@alet_turu", aletTuru);
                        cmd.Parameters.AddWithValue("@fiyat", fiyat);

                        // Yeni alet ID'sini alýyoruz
                        int yeniAletID = (int)cmd.ExecuteScalar();

                        // Spor_Aletleri'ne ekleme
                        string querySpor = "INSERT INTO \"Spor_Aletleri\" (alet_id, miktar) VALUES (@alet_id, @miktar);";
                        using (NpgsqlCommand cmdSpor = new NpgsqlCommand(querySpor, conn))
                        {
                            cmdSpor.Parameters.AddWithValue("@alet_id", yeniAletID);
                            cmdSpor.Parameters.AddWithValue("@miktar", miktar);
                            cmdSpor.ExecuteNonQuery();
                        }

                        MessageBox.Show("Spor aleti baþarýyla eklendi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        VerileriYukle(); // DataGridView yenileme
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Alanlarý temizle
            txtAletAdi.Clear();
            txtAletTuru.Clear();
            txtFiyat.Clear();
            txtMiktar.Clear();
        }

        // Spor aleti silme fonksiyonu
        private void btnAletSil_Click(object sender, EventArgs e)
        {
            // Alet ID'sinin geçerli olup olmadýðýný kontrol et
            if (!int.TryParse(txtAletID.Text, out int aletID) || aletID <= 0)
            {
                MessageBox.Show("Lütfen geçerli bir Alet ID giriniz!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Aletin bilgilerini al
            try
            {
                string queryAletBilgi = "SELECT alet_adi, alet_turu, fiyat FROM \"Temel_Spor_Aleti\" WHERE alet_id = @alet_id;";

                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    using (NpgsqlCommand cmd = new NpgsqlCommand(queryAletBilgi, conn))
                    {
                        cmd.Parameters.AddWithValue("@alet_id", aletID);
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string aletAdi = reader["alet_adi"].ToString();
                                string aletTuru = reader["alet_turu"].ToString();
                                decimal fiyat = Convert.ToDecimal(reader["fiyat"]);

                                DialogResult dialogResult = MessageBox.Show(
                                    $"Alet Adý: {aletAdi}\nAlet Türü: {aletTuru}\nFiyat: {fiyat:C}\nBu aleti silmek istediðinizden emin misiniz?",
                                    "Alet Silme Onayý",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Question
                                );

                                if (dialogResult == DialogResult.Yes)
                                {
                                    // Alet silme iþlemi
                                    string querySil = "DELETE FROM \"Spor_Aletleri\" WHERE alet_id = @alet_id; " +
                                                      "DELETE FROM \"Temel_Spor_Aleti\" WHERE alet_id = @alet_id;";

                                    using (NpgsqlConnection connSil = new NpgsqlConnection(connectionString))
                                    {
                                        connSil.Open();
                                        using (NpgsqlCommand cmdSil = new NpgsqlCommand(querySil, connSil))
                                        {
                                            cmdSil.Parameters.AddWithValue("@alet_id", aletID);
                                            int rowsAffected = cmdSil.ExecuteNonQuery();

                                            if (rowsAffected > 0)
                                            {
                                                MessageBox.Show("Spor aleti baþarýyla silindi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                                VerileriYukle(); // DataGridView yenileme
                                            }
                                            else
                                            {
                                                MessageBox.Show("Alet silinirken bir hata oluþtu.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                MessageBox.Show("Belirtilen ID'ye sahip bir spor aleti bulunamadý.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Alet ID alanýný temizle
            txtAletID.Clear();
        }


        private void LoadPaketler()
        {
            // ComboBox'ý temizle
            cbPaketSecimi1.Items.Clear();

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                // Paketleri al
                string query = "SELECT paket_id, paket_adi FROM \"Uyelik_Paketleri\"";
                using (var cmd = new NpgsqlCommand(query, connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var paket = new
                            {
                                PaketId = reader.GetInt32(0),
                                PaketAdi = reader.GetString(1)
                            };

                            // ComboBox'a PaketAdi ekleniyor
                            cbPaketSecimi1.Items.Add(paket);
                        }
                    }
                }
            }

            // ComboBox'ta seçilen öðenin ne olacaðýný belirlemek için ToString metodu
            cbPaketSecimi1.DisplayMember = "PaketAdi";
            cbPaketSecimi1.ValueMember = "PaketId";

            // Ýlk item seçili yapmak (isteðe baðlý)
            if (cbPaketSecimi1.Items.Count > 0)
            {
                cbPaketSecimi1.SelectedIndex = 0;
            }
        }

        private void LoadOdemeData()
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                // Düzeltilmiþ sorgu: Uyeler ve Insan tablolarý ile birlikte Uye_Odemeleri tablosu birleþtiriliyor.
                string query = @"
    SELECT 
        o.odeme_id, 
        i.tc_numarasi, 
        i.ad, 
        i.soyad, 
        o.odeme_tarihi, 
        o.tutar 
    FROM ""Uye_Odemeleri"" o
    INNER JOIN ""Uyeler"" u ON o.uye_id = u.uye_id
    INNER JOIN ""Insan"" i ON u.insan_id = i.insan_id";

                using (var cmd = new NpgsqlCommand(query, connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        // DataTable oluþturun ve verileri ekleyin
                        DataTable dt = new DataTable();
                        dt.Load(reader);

                        // DataGridView'e veri baðlama
                        dgvOdeme.DataSource = dt;
                    }
                }
            }
        }

        // Paket seçimi deðiþtiðinde fiyat ve giriþ limiti etiketlerini güncelle
        private void cbPaketSecimi1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbPaketSecimi1.SelectedIndex != -1)
            {
                // Seçilen paket ID'sini al
                int paketId = ((dynamic)cbPaketSecimi1.SelectedItem).PaketId;

                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    // Seçilen paket için fiyat ve giriþ limitini al
                    string query = "SELECT fiyat, giris_limiti FROM \"Uyelik_Paketleri\" WHERE paket_id = @paket_id";
                    using (var cmd = new NpgsqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@paket_id", paketId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                decimal fiyat = reader.GetDecimal(0);
                                int girisLimit = reader.GetInt32(1);

                                // Fiyat ve giriþ limitini etiketlere yaz
                                lblPaketFiyat1.Text = $"Fiyat: {fiyat:C}";
                                lblGirisLimit1.Text = $"Giriþ Limiti: {girisLimit}";
                            }
                        }
                    }
                }
            }
            else
            {
                // Paket seçilmediyse etiketleri temizle
                lblPaketFiyat1.Text = "Fiyat: -";
                lblGirisLimit1.Text = "Giriþ Limiti: -";
            }
        }

        // Ödeme yapma butonuna týklandýðýnda çalýþacak metod
        private void btnOdemeYap_Click(object sender, EventArgs e)
        {
            string tcNumarasi = txtTcNumarasi.Text.Trim();

            // TC numarasýnýn 11 haneli ve rakamlardan oluþup oluþmadýðýný kontrol et
            if (tcNumarasi.Length != 11 || !tcNumarasi.All(char.IsDigit))
            {
                MessageBox.Show("TC numarasý 11 haneli ve sadece rakamlardan oluþmalýdýr.");
                return;
            }

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                // TC numarasýna ait üyenin olup olmadýðýný kontrol et
                string uyeKontrolQuery = @"
            SELECT COUNT(*) 
            FROM ""Uyeler"" u
            INNER JOIN ""Insan"" i ON u.insan_id = i.insan_id
            WHERE i.tc_numarasi = @tc_numarasi";
                using (var cmd = new NpgsqlCommand(uyeKontrolQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@tc_numarasi", tcNumarasi);
                    int uyeSayisi = Convert.ToInt32(cmd.ExecuteScalar());

                    if (uyeSayisi == 0)
                    {
                        MessageBox.Show("Bu TC numarasýna sahip bir üye bulunamadý.");
                        return;
                    }
                }

                // Paket seçimini al
                if (cbPaketSecimi1.SelectedIndex == -1)
                {
                    MessageBox.Show("Lütfen bir paket seçin.");
                    return;
                }

                int paketId = ((dynamic)cbPaketSecimi1.SelectedItem).PaketId;

                // Seçilen paketin fiyatýný al
                string fiyatQuery = "SELECT fiyat, giris_limiti FROM \"Uyelik_Paketleri\" WHERE paket_id = @paket_id";
                decimal fiyat = 0;
                int girisLimiti = 0;

                using (var cmd = new NpgsqlCommand(fiyatQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@paket_id", paketId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            fiyat = reader.GetDecimal(0);
                            girisLimiti = reader.GetInt32(1);
                        }
                    }
                }

                // Üye bilgilerini al
                string uyeQuery = @"
            SELECT u.uye_id, u.kalan_giris 
            FROM ""Uyeler"" u
            INNER JOIN ""Insan"" i ON u.insan_id = i.insan_id
            WHERE i.tc_numarasi = @tc_numarasi";
                int uyeId = 0;
                int mevcutGiris = 0;

                using (var cmd = new NpgsqlCommand(uyeQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@tc_numarasi", tcNumarasi);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            uyeId = reader.GetInt32(0);
                            mevcutGiris = reader.GetInt32(1);
                        }
                    }
                }

                // Ödeme kaydýný oluþtur
                string odemeQuery = "INSERT INTO \"Uye_Odemeleri\" (uye_id, tutar, paket_id) VALUES (@uye_id, @tutar, @paket_id)";
                using (var cmd = new NpgsqlCommand(odemeQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@uye_id", uyeId);
                    cmd.Parameters.AddWithValue("@tutar", fiyat);
                    cmd.Parameters.AddWithValue("@paket_id", paketId);
                    cmd.ExecuteNonQuery();
                }

                // DataGridView'i güncelle
                LoadOdemeData();
                ListeleUyeler();
                txtTcNumarasi.Clear();
                cbPaketSecimi1.SelectedIndex = -1;
                MessageBox.Show("Ödeme baþarýyla tamamlandý.");
            }
        }



        private void AktifUyeleriGetir()
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                string query = @"
    SELECT 
        i.ad AS ""Ad"", 
        i.soyad AS ""Soyad"", 
        a.aktif_baslangic AS ""Giriþ Saati"", 
        a.aktif_bitis AS ""Çýkýþ Saati"",
        EXTRACT(EPOCH FROM (COALESCE(a.aktif_bitis, NOW()) - a.aktif_baslangic)) / 60 AS ""Kullaným Süresi (Dakika)""
    FROM ""Aktif_Uyeler"" a
    INNER JOIN ""Uyeler"" u ON a.uye_id = u.uye_id
    INNER JOIN ""Insan"" i ON u.insan_id = i.insan_id
    WHERE a.aktif_bitis IS NULL OR a.aktif_baslangic IS NOT NULL";

                NpgsqlDataAdapter dataAdapter = new NpgsqlDataAdapter(query, connection);
                DataTable dataTable = new DataTable();
                dataAdapter.Fill(dataTable);

                dgvAktifUyeler.DataSource = dataTable;
            }
        }



        private void AktifKisiSayisiGuncelle()
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                // Aktif kiþi sayýsýnýn son kaydýný alýyoruz
                string query = @"
            SELECT aktif_sayisi 
            FROM ""Aktif_Kisi_Sayisi""
            ORDER BY tarih DESC
            LIMIT 1";  // Sadece son kaydý alýyoruz (en son aktif kiþi sayýsý)

                NpgsqlCommand command = new NpgsqlCommand(query, connection);

                try
                {
                    connection.Open();

                    // Aktif kiþi sayýsýný alýyoruz
                    object result = command.ExecuteScalar();
                    int aktifKisiSayisi = result != DBNull.Value ? Convert.ToInt32(result) : 0;

                    // Aktif kiþi sayýsýný etiket üzerinde gösteriyoruz
                    //lblAktifKisiSayisi.Text = $"Þu anda içeride: {aktifKisiSayisi} kiþi";
                }
                catch (Exception ex)
                {
                    // Hata durumunda loglama yapabiliriz veya kullanýcýyý bilgilendirebiliriz
                    Console.WriteLine($"Hata: {ex.Message}");
                }
            }
        }











        private void btnVerileriYenile_Click(object sender, EventArgs e)
        {
            AktifUyeleriGetir();
            AktifKisiSayisiGuncelle();
        }


        // Puanlarý listele butonu için kod
        private void btnListele1_Click(object sender, EventArgs e)
        {
            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                SELECT 
                    i.tc_numarasi AS ""TC"",
                    i.ad || ' ' || i.soyad AS ""Ad Soyad"", 
                    COALESCE(p.puan, 0) AS ""Puan"",         
                    g.kalan_giris AS ""Giriþ Hakký""
                FROM ""Insan"" i
                JOIN ""Uyeler"" u ON i.insan_id = u.insan_id
                LEFT JOIN ""Puanlar"" p ON u.uye_id = p.uye_id   
                LEFT JOIN ""Giris_Limitleri"" g ON u.uye_id = g.uye_id
                ORDER BY ""Puan"" DESC";

                    NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(query, conn);
                    DataTable table = new DataTable();
                    adapter.Fill(table);

                    if (table.Rows.Count > 0)
                    {
                        dataGridView1.DataSource = table;
                        dataGridView1.Refresh();  // Veriyi yenile
                    }
                    else
                    {
                        MessageBox.Show("Veri bulunamadý.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata oluþtu: {ex.Message}");
            }
        }

        private void btnAra1_Click(object sender, EventArgs e)
        {
            string tc = textBoxTc.Text.Trim();

            if (tc.Length != 11 || !long.TryParse(tc, out _))
            {
                MessageBox.Show("Lütfen geçerli bir 11 haneli TC kimlik numarasý giriniz!");
                return;
            }

            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                SELECT 
                    i.tc_numarasi AS ""TC"",
                    i.ad || ' ' || i.soyad AS ""Ad Soyad"",  
                    COALESCE(p.puan, 0) AS ""Puan"",         
                    g.kalan_giris AS ""Giriþ Hakký""
                FROM ""Insan"" i
                JOIN ""Uyeler"" u ON i.insan_id = u.insan_id
                LEFT JOIN ""Puanlar"" p ON u.uye_id = p.uye_id   
                LEFT JOIN ""Giris_Limitleri"" g ON u.uye_id = g.uye_id
                WHERE i.tc_numarasi = @tc
                ORDER BY ""Puan"" DESC";

                    NpgsqlCommand cmd = new NpgsqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@tc", tc);

                    NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(cmd);
                    DataTable table = new DataTable();
                    adapter.Fill(table);

                    if (table.Rows.Count > 0)
                    {
                        dataGridView1.DataSource = table;
                        dataGridView1.Refresh();  // Veriyi yenile
                    }
                    else
                    {
                        MessageBox.Show("Girilen TC kimlik numarasýna ait üye bulunamadý.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata oluþtu: {ex.Message}");
            }
        }



        private void btnAraGuncelle_Click(object sender, EventArgs e)
        {
            // TC numarasýnýn boþ olmadýðýný kontrol et
            if (string.IsNullOrWhiteSpace(txtTcGuncelle.Text))
            {
                MessageBox.Show("Lütfen TC numarasýný girin.");
                return;
            }

            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL fonksiyonunu çaðýrarak veriyi alýyoruz
                    using (var cmd = new NpgsqlCommand("SELECT * FROM get_insan_bilgileri(@tc_numarasi)", connection))
                    {
                        cmd.Parameters.AddWithValue("@tc_numarasi", txtTcGuncelle.Text);
                        var reader = cmd.ExecuteReader();
                        if (reader.Read())
                        {
                            // Kiþinin adý soyadý, doðum tarihi, telefon ve email bilgilerini formda göster
                            lblKisiAdiSoyadi.Text = $"{reader["ad"]} {reader["soyad"]}";
                            txtAdGuncelle.Text = reader["ad"].ToString();
                            txtSoyadGuncelle.Text = reader["soyad"].ToString();
                            dtpDogumTarihiGuncelle.Value = Convert.ToDateTime(reader["dogum_tarihi"]);
                            txtTelGuncelle.Text = reader["telefon"].ToString();
                            txtEmailGuncelle.Text = reader["email"].ToString();
                        }
                        else
                        {
                            MessageBox.Show("Girilen TC numarasýna ait üye bulunamadý.");
                            lblKisiAdiSoyadi.Text = "";
                        }
                    }

                }
            }
            catch (PostgresException ex)
            {
                MessageBox.Show("Hata: " + ex.Message);
            }
        }



        private void btnGuncelle_Click(object sender, EventArgs e)
        {
            // Gerekli alanlarýn doldurulduðunu kontrol et
            if (string.IsNullOrWhiteSpace(txtAdGuncelle.Text) ||
                string.IsNullOrWhiteSpace(txtSoyadGuncelle.Text) ||
                string.IsNullOrWhiteSpace(txtTelGuncelle.Text) ||
                string.IsNullOrWhiteSpace(txtEmailGuncelle.Text))
            {
                MessageBox.Show("Lütfen tüm bilgileri doldurun.");
                return;
            }

            // Telefon numarasýnýn formatýný kontrol et
            if (!System.Text.RegularExpressions.Regex.IsMatch(txtTelGuncelle.Text, @"^\d{10}$"))
            {
                MessageBox.Show("Geçerli bir telefon numarasý girin.");
                return;
            }

            // E-posta adresinin formatýný kontrol et
            try
            {
                var mailAddress = new System.Net.Mail.MailAddress(txtEmailGuncelle.Text);
            }
            catch (FormatException)
            {
                MessageBox.Show("Geçerli bir e-posta adresi girin.");
                return;
            }

            // Yaþ kontrolü (18 yaþýndan büyük olmalý)
            DateTime dogumTarihi = dtpDogumTarihiGuncelle.Value;
            int yas = DateTime.Now.Year - dogumTarihi.Year;
            if (DateTime.Now.DayOfYear < dogumTarihi.DayOfYear)
                yas--;

            if (yas < 18)
            {
                MessageBox.Show("Üye 18 yaþýndan küçük olamaz.");
                return;
            }

            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    // TC numarasýna göre insan_id'yi al
                    int insanId = 0;
                    using (var cmdGetInsanId = new NpgsqlCommand("SELECT insan_id FROM \"Insan\" WHERE tc_numarasi = @tc_numarasi LIMIT 1", connection))
                    {
                        cmdGetInsanId.Parameters.Add("@tc_numarasi", NpgsqlDbType.Text).Value = txtTcGuncelle.Text;
                        var result = cmdGetInsanId.ExecuteScalar();

                        if (result != null)
                        {
                            insanId = Convert.ToInt32(result);
                        }
                        else
                        {
                            MessageBox.Show("Girilen TC numarasýna ait üye bulunamadý.");
                            return;
                        }
                    }

                    // "Insan" tablosundaki kiþisel bilgileri güncelle
                    using (var cmdUpdateInsan = new NpgsqlCommand("UPDATE \"Insan\" SET ad = @ad, soyad = @soyad, dogum_tarihi = @dogum_tarihi, telefon = @telefon, email = @email WHERE insan_id = @insan_id", connection))
                    {
                        cmdUpdateInsan.Parameters.Add("@ad", NpgsqlDbType.Text).Value = txtAdGuncelle.Text;
                        cmdUpdateInsan.Parameters.Add("@soyad", NpgsqlDbType.Text).Value = txtSoyadGuncelle.Text;
                        cmdUpdateInsan.Parameters.Add("@dogum_tarihi", NpgsqlDbType.Date).Value = dtpDogumTarihiGuncelle.Value.Date;
                        cmdUpdateInsan.Parameters.Add("@telefon", NpgsqlDbType.Text).Value = txtTelGuncelle.Text;
                        cmdUpdateInsan.Parameters.Add("@email", NpgsqlDbType.Text).Value = txtEmailGuncelle.Text;
                        cmdUpdateInsan.Parameters.Add("@insan_id", NpgsqlDbType.Integer).Value = insanId;

                        cmdUpdateInsan.ExecuteNonQuery();
                    }

                    // Güncelleme baþarýlý olduðunda kullanýcýya bilgi ver
                    MessageBox.Show("Üye baþarýyla güncellendi.");
                    lblKisiAdiSoyadi.Text = "Güncelleme baþarýlý.";

                    // Formu temizle
                    txtTcGuncelle.Clear();
                    txtAdGuncelle.Clear();
                    txtSoyadGuncelle.Clear();
                    txtTelGuncelle.Clear();
                    txtEmailGuncelle.Clear();
                    dtpDogumTarihiGuncelle.Value = DateTime.Now;

                    // Label'ý temizle
                    lblKisiAdiSoyadi.Text = "";
                    ListeleUyeler();
                }
            }
            catch (PostgresException ex)
            {
                MessageBox.Show("Hata: " + ex.Message);
            }
        }



        // Bilgilendirmeleri veritabanýndan yükle
        private void LoadBilgilendirmeler()
        {
            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open(); // Baðlantýyý açýyoruz
                    string query = "SELECT * FROM \"Bilgilendirme\""; // Tablo adýný doðru yazdýk
                    NpgsqlDataAdapter dataAdapter = new NpgsqlDataAdapter(query, connection);
                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable); // Verileri dolduruyoruz
                    dataGridViewBilgilendirme.DataSource = dataTable; // DataGridView'e baðlýyoruz
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata oluþtu: {ex.Message}"); // Hata mesajýný göster
            }
        }



        // Yeni bilgilendirme ekleme
        private void btnEkle_Click(object sender, EventArgs e)
        {
            string baslik = txtBaslik.Text.Trim();
            string aciklama = txtAciklama.Text.Trim();
            DateTime gecerlilikTarihi = dateTimePickerGecerlilik.Value;

            // Baþlýk ve açýklama boþ olup olmadýðýný kontrol et
            if (string.IsNullOrEmpty(baslik) || string.IsNullOrEmpty(aciklama))
            {
                MessageBox.Show("Baþlýk ve açýklama boþ olamaz.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    string query = "INSERT INTO \"Bilgilendirme\" (baslik, aciklama, gecerlilik_tarihi) VALUES (@Baslik, @Aciklama, @GecerlilikTarihi)";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Baslik", baslik);
                        command.Parameters.AddWithValue("@Aciklama", aciklama);
                        command.Parameters.AddWithValue("@GecerlilikTarihi", gecerlilikTarihi);

                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                }

                LoadBilgilendirmeler(); // Listeyi güncelle
                MessageBox.Show("Bilgilendirme baþarýyla eklendi.", "Baþarýlý", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata oluþtu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        // Seçilen bilgilendirmeyi silme
        private void btnSil_Click(object sender, EventArgs e)
        {
            string bilgilendirmeIdText = txtBilgilendirmeId.Text.Trim();

            // ID boþ mu kontrol et
            if (string.IsNullOrEmpty(bilgilendirmeIdText))
            {
                MessageBox.Show("Silmek için ID girin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // ID geçerli mi kontrol et
            if (!int.TryParse(bilgilendirmeIdText, out int bilgilendirmeId))
            {
                MessageBox.Show("Geçerli bir ID girin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL fonksiyonunu çaðýr
                    string query = "SELECT sil_bilgilendirme(@BilgilendirmeId)";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@BilgilendirmeId", bilgilendirmeId);
                        command.ExecuteNonQuery();
                    }
                }

                LoadBilgilendirmeler(); // Listeyi güncelle
                MessageBox.Show("Bilgilendirme baþarýyla silindi.", "Baþarýlý", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata oluþtu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Form2 ve Form3'ü kontrol edip kapatýyoruz
            if (form2 != null && !form2.IsDisposed)
            {
                form2.Close();
            }

            if (form3 != null && !form3.IsDisposed)
            {
                form3.Close();
            }
        }
    }


}


