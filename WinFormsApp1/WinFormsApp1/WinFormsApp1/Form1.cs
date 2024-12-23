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
        // PostgreSQL veritaban� ba�lant� dizesi
        private string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=can12345;Database=sporSalonu";

        public Form1()
        {

            // Form2 ve Form3'� ba�lat�yoruz
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



        // �yeleri listelemek i�in
        // �yeleri listelemek i�in
        private void ListeleUyeler()
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    // Uyeler ile Insan tablolar�n� birle�tirerek gerekli bilgileri se�iyoruz.
                    string query = @"
SELECT 
    u.uye_id,
    i.tc_numarasi,
    i.ad,
    i.soyad,
    i.dogum_tarihi,  -- Do�um tarihi eklendi
    i.email,
    i.telefon,
    i.kayit_tarihi,
    u.durum,
    u.kalan_giris,
    -- Puanlar tablosundaki puan de�erini al�yoruz. E�er yoksa 0 d�nd�r�l�r.
    COALESCE(p.puan, 0) AS puan
FROM ""Uyeler"" u
INNER JOIN ""Insan"" i ON u.insan_id = i.insan_id
LEFT JOIN ""Puanlar"" p ON u.uye_id = p.uye_id";  // LEFT JOIN ile Puanlar tablosundan veri al�yoruz.

                    NpgsqlDataAdapter dataAdapter = new NpgsqlDataAdapter(query, connection);
                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);
                    dgvUyeler.DataSource = dataTable;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata olu�tu: {ex.Message}");
            }
        }








        // �yeleri TC numaras�na g�re aramak i�in
        // �yeleri TC numaras�na g�re aramak i�in
        private void AraUyeler()
        {
            if (string.IsNullOrWhiteSpace(txtTcAra.Text))
            {
                MessageBox.Show("L�tfen ge�erli bir TC numaras� girin.", "Uyar�", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var connection = new NpgsqlConnection(connectionString))
            {
                try
                {
                    connection.Open(); // Ba�lant�y� a�

                    // Insan ve Uyeler tablolar�n� ili�kilendiren sorgu
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
    -- Puanlar tablosundan puan bilgisi �ekiliyor.
    COALESCE(p.puan, 0) AS puan
FROM ""Insan"" i
INNER JOIN ""Uyeler"" u ON i.insan_id = u.insan_id
LEFT JOIN ""Puanlar"" p ON u.uye_id = p.uye_id  -- Puanlar tablosunu LEFT JOIN ile dahil ediyoruz
WHERE i.tc_numarasi = @tc_numarasi";  // Girilen TC numaras�na g�re sorgulama yap�yoruz

                    // Sorguyu �al��t�rmak i�in DataAdapter kullan�m�
                    NpgsqlDataAdapter dataAdapter = new NpgsqlDataAdapter(query, connection);
                    dataAdapter.SelectCommand.Parameters.AddWithValue("@tc_numarasi", txtTcAra.Text.Trim());

                    // Gelen verileri DataTable'a aktar
                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);

                    // Sonu�lar� kontrol et
                    if (dataTable.Rows.Count == 0)
                    {
                        MessageBox.Show("Girilen TC numaras�na ait bir �ye bulunamad�. T�m �yeler listeleniyor.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ListeleUyeler(); // T�m �yeleri yeniden listele
                    }
                    else
                    {
                        dgvUyeler.DataSource = null; // �nceki referans� temizle
                        dgvUyeler.DataSource = dataTable; // Sonu�lar� DataGridView'de g�ster
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Bir hata olu�tu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }





        // TC numaras�na g�re arama yapma
        private void btnAra_Click(object sender, EventArgs e)
        {
            AraUyeler(); // TC numaras�na g�re arama yap
            txtTcAra.Clear();
        }

        // Paketleri ComboBox'a y�kleme
        private void PaketleriYukle()
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                // Paketleri "Uyelik_Paketleri" tablosundan se�iyoruz
                string query = "SELECT paket_id, paket_adi FROM \"Uyelik_Paketleri\"";
                using (var cmd = new NpgsqlCommand(query, connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        // ComboBox'� temizle
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

            // ComboBox DisplayMember ve ValueMember ayarlar�n� yap
            cbPaketSecimi.DisplayMember = "PaketAdi";
            cbPaketSecimi.ValueMember = "PaketId";
        }

        // Paket se�imi de�i�ti�inde fiyat ve giri� limitini g�ncelleyen event handler
        private void cbPaketSecimi_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbPaketSecimi.SelectedIndex != -1)
            {
                // Se�ilen paketin ID'sini al
                int paketId = ((dynamic)cbPaketSecimi.SelectedItem).PaketId;

                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    // Se�ilen paket i�in fiyat ve giri� limitini al
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

                                // Fiyat ve giri� limitini etiketlere yaz
                                //lblPaketFiyat.Text = $"Fiyat: {fiyat:C}";
                                //lblGirisLimit.Text = $"Giri� Limiti: {girisLimit}";
                            }
                        }
                    }
                }
            }
            else
            {
                // Paket se�ilmediyse etiketleri temizle
                //lblPaketFiyat.Text = "Fiyat: -";
                //lblGirisLimit.Text = "Giri� Limiti: -";
            }
        }


        // �ye ekleme butonuna t�klan�nca �al��acak fonksiyon
        private void btnUyeEkle_Click(object sender, EventArgs e)
        {
            // Bo� alan kontrol�
            if (string.IsNullOrWhiteSpace(txtTc.Text) || string.IsNullOrWhiteSpace(txtAd.Text) ||
                string.IsNullOrWhiteSpace(txtSoyad.Text) || cbPaketSecimi.SelectedIndex == -1 ||
                string.IsNullOrWhiteSpace(txtEmail.Text) || string.IsNullOrWhiteSpace(txtTelefon.Text))
            {
                MessageBox.Show("L�tfen t�m alanlar� doldurun.");
                return;
            }

            // TC numaras�n�n 11 basamaktan olu�up olu�mad���n� kontrol et
            if (txtTc.Text.Length != 11 || !txtTc.Text.All(char.IsDigit))
            {
                MessageBox.Show("TC numaras� 11 haneli ve sadece rakamlardan olu�mal�d�r.");
                return;
            }

            // Ya� hesaplama
            int yas = DateTime.Now.Year - dtpDogumTarihi.Value.Year;
            if (dtpDogumTarihi.Value > DateTime.Now.AddYears(-yas)) yas--;

            // 18 ya��ndan k���k �yeleri eklememek i�in kontrol
            if (yas < 18)
            {
                MessageBox.Show("�ye ya�� 18'den k���k olamaz.");
                return;
            }

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                // Ayn� TC numaras�yla kay�t olup olmad���n� kontrol et
                string kontrolQuery = "SELECT COUNT(*) FROM \"Insan\" WHERE tc_numarasi = @tc_numarasi";
                using (var kontrolCmd = new NpgsqlCommand(kontrolQuery, connection))
                {
                    kontrolCmd.Parameters.AddWithValue("@tc_numarasi", txtTc.Text);
                    int insanSayisi = Convert.ToInt32(kontrolCmd.ExecuteScalar());
                    if (insanSayisi > 0)
                    {
                        MessageBox.Show("Bu TC numaras�yla zaten bir kay�t var.");
                        return;
                    }
                }

                // `Insan` tablosuna kay�t ekleme, do�um tarihini de ekliyoruz
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
                    insanCmd.Parameters.AddWithValue("@dogum_tarihi", dtpDogumTarihi.Value);  // Do�um tarihi parametresini ekledik
                    insanId = Convert.ToInt32(insanCmd.ExecuteScalar());
                }

                // Se�ilen paket ID'sini al
                int paketId = ((dynamic)cbPaketSecimi.SelectedItem).PaketId;

                // `Uyeler` tablosuna kay�t ekleme
                string uyeQuery = "INSERT INTO \"Uyeler\" (insan_id, durum, kalan_giris) " +
                                  "VALUES (@insan_id, 'Aktif', " +
                                  "(SELECT giris_limiti FROM \"Uyelik_Paketleri\" WHERE paket_id = @paket_id))";
                using (var uyeCmd = new NpgsqlCommand(uyeQuery, connection))
                {
                    uyeCmd.Parameters.AddWithValue("@insan_id", insanId);
                    uyeCmd.Parameters.AddWithValue("@paket_id", paketId);
                    uyeCmd.ExecuteNonQuery();
                }

                MessageBox.Show("�ye ba�ar�yla eklendi.");
            }

            // �yeleri listeleme
            ListeleUyeler();
            txtTc.Clear();
            txtAd.Clear();
            txtSoyad.Clear();
            txtEmail.Clear();
            txtTelefon.Clear();
            dtpDogumTarihi.Value = DateTime.Now;
            cbPaketSecimi.SelectedIndex = -1;
        }



        // �ye silme butonuna t�klan�nca �al��acak fonksiyon
        private void btnUyeSil_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTcSil.Text))
            {
                MessageBox.Show("L�tfen silmek istedi�iniz �yenin TC numaras�n� girin.", "Uyar�", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                // `tc_numarasi` �zerinden `insan_id`yi al
                string insanQuery = "SELECT insan_id FROM \"Insan\" WHERE tc_numarasi = @tc_numarasi";
                int insanId;
                using (var insanCmd = new NpgsqlCommand(insanQuery, connection))
                {
                    insanCmd.Parameters.AddWithValue("@tc_numarasi", txtTcSil.Text);
                    var result = insanCmd.ExecuteScalar();
                    if (result == null)
                    {
                        MessageBox.Show("Girilen TC numaras�na ait bir kay�t bulunamad�.");
                        return;
                    }
                    insanId = Convert.ToInt32(result);
                }

                // `Uyeler` tablosundan silme i�lemi
                string uyeDeleteQuery = "DELETE FROM \"Uyeler\" WHERE insan_id = @insan_id";
                using (var uyeDeleteCmd = new NpgsqlCommand(uyeDeleteQuery, connection))
                {
                    uyeDeleteCmd.Parameters.AddWithValue("@insan_id", insanId);
                    uyeDeleteCmd.ExecuteNonQuery();
                }

                // `Insan` tablosundan silme i�lemi
                string insanDeleteQuery = "DELETE FROM \"Insan\" WHERE insan_id = @insan_id";
                using (var insanDeleteCmd = new NpgsqlCommand(insanDeleteQuery, connection))
                {
                    insanDeleteCmd.Parameters.AddWithValue("@insan_id", insanId);
                    insanDeleteCmd.ExecuteNonQuery();
                }

                MessageBox.Show("�ye ba�ar�yla silindi.");
            }
            ListeleUyeler(); // �yeleri yeniden listele
            txtTcSil.Clear();
        }




        // Giri� yap butonuna t�klan�nca �al��acak fonksiyon
        private void btnGirisYap_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTcGiris.Text))
            {
                MessageBox.Show("L�tfen TC numaras�n� girin.");
                return;
            }

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                // TC numaras� �zerinden �ye bilgilerini al
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
                        int uyeId = reader.GetInt32(0); // �yenin ID'si
                        string durum = reader.GetString(1); // �yenin durumu
                        int kalanGiris = reader.GetInt32(2); // Kalan giri� hakk�

                        reader.Close(); // Reader'� kapat

                        // Durum ve giri� hakk� kontrol�
                        if (durum == "Aktif")
                        {
                            MessageBox.Show("�ye zaten aktif, tekrar giri� yap�lamaz.");
                            return;
                        }
                        else if (kalanGiris <= 0)
                        {
                            MessageBox.Show("�yenin giri� hakk� kalmam��.");
                            return;
                        }

                        // Puan kontrol� ve �d�l i�lemi
                        string puanQuery = "SELECT puan FROM \"Puanlar\" WHERE uye_id = @uye_id";
                        using (var puanCmd = new NpgsqlCommand(puanQuery, connection))
                        {
                            puanCmd.Parameters.AddWithValue("@uye_id", uyeId);
                            var puanResult = puanCmd.ExecuteScalar();
                            int puan = puanResult != null ? Convert.ToInt32(puanResult) : 0;

                            if (puan >= 500)
                            {
                                // 500 puan d���r ve 5 giri� hakk� ekle
                                string updatePuanQuery = "UPDATE \"Puanlar\" SET puan = puan - 500 WHERE uye_id = @uye_id";
                                string updateGirisQuery = "UPDATE \"Uyeler\" SET kalan_giris = kalan_giris + 5 WHERE uye_id = @uye_id";

                                using (var updatePuanCmd = new NpgsqlCommand(updatePuanQuery, connection))
                                using (var updateGirisCmd = new NpgsqlCommand(updateGirisQuery, connection))
                                {
                                    updatePuanCmd.Parameters.AddWithValue("@uye_id", uyeId);
                                    updateGirisCmd.Parameters.AddWithValue("@uye_id", uyeId);

                                    updatePuanCmd.ExecuteNonQuery();
                                    updateGirisCmd.ExecuteNonQuery();

                                    MessageBox.Show("�yenin 500 puan hediye olarak 5 giri� hakk� ile de�i�tirildi.");
                                }
                            }
                        }

                        // �yeyi aktif yap
                        string updateQuery = "UPDATE \"Uyeler\" SET durum = 'Aktif' WHERE uye_id = @uye_id";
                        using (var updateCmd = new NpgsqlCommand(updateQuery, connection))
                        {
                            updateCmd.Parameters.AddWithValue("@uye_id", uyeId);
                            updateCmd.ExecuteNonQuery();
                        }

                        // Aktif �yeler tablosuna giri� kayd�n� ekle
                        string aktifUyeQuery = "INSERT INTO \"Aktif_Uyeler\" (uye_id, aktif_baslangic) VALUES (@uye_id, CURRENT_TIMESTAMP)";
                        using (var aktifCmd = new NpgsqlCommand(aktifUyeQuery, connection))
                        {
                            aktifCmd.Parameters.AddWithValue("@uye_id", uyeId);
                            aktifCmd.ExecuteNonQuery();
                        }

                        // Girilenler tablosuna giri� kayd�n� ekle
                        string girilenlerQuery = "INSERT INTO \"Girilenler\" (uye_id, giris_tarihi) VALUES (@uye_id, CURRENT_TIMESTAMP)";
                        using (var girilenlerCmd = new NpgsqlCommand(girilenlerQuery, connection))
                        {
                            girilenlerCmd.Parameters.AddWithValue("@uye_id", uyeId);
                            girilenlerCmd.ExecuteNonQuery();
                        }

                        // Aktif ki�i say�s�n� g�ncelle
                        AktifKisiSayisiGuncelle();

                        MessageBox.Show("Giri� ba�ar�l�.");
                    }
                    else
                    {
                        MessageBox.Show("�ye bulunamad�.");
                    }
                }
            }
            ListeleUyeler();
            txtTcGiris.Clear();
        }







        // ��k�� yap butonuna t�klan�nca �al��acak fonksiyon
        private void btnCikisYap_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTcCikis.Text))
            {
                MessageBox.Show("L�tfen ��k�� yapacak �yenin TC numaras�n� girin.");
                return;
            }

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                // TC numaras� �zerinden �ye bilgilerini al
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
                        int uyeId = reader.GetInt32(0);  // �yenin ID'si
                        string durum = reader.GetString(1);  // �yenin durumu

                        // Durum kontrol�: �ye aktif mi?
                        if (durum == "Pasif")
                        {
                            MessageBox.Show("�ye zaten pasif, ��k�� yap�lamaz.");
                        }
                        else
                        {
                            reader.Close();

                            // �yeyi pasif yap
                            string updateQuery = "UPDATE \"Uyeler\" SET durum = 'Pasif' WHERE uye_id = @uye_id";
                            using (var updateCmd = new NpgsqlCommand(updateQuery, connection))
                            {
                                updateCmd.Parameters.AddWithValue("@uye_id", uyeId);
                                updateCmd.ExecuteNonQuery();
                            }

                            // Aktif �yeler tablosunda ��k�� saatini g�ncelle
                            string aktifUyeUpdateQuery = "UPDATE \"Aktif_Uyeler\" SET aktif_bitis = CURRENT_TIMESTAMP WHERE uye_id = @uye_id AND aktif_bitis IS NULL";
                            using (var aktifUpdateCmd = new NpgsqlCommand(aktifUyeUpdateQuery, connection))
                            {
                                aktifUpdateCmd.Parameters.AddWithValue("@uye_id", uyeId);
                                aktifUpdateCmd.ExecuteNonQuery();
                            }

                            // ��k�� tablosuna kay�t ekle
                            string cikisQuery = "INSERT INTO \"Cikis\" (uye_id, cikis_tarihi) VALUES (@uye_id, CURRENT_TIMESTAMP)";
                            using (var cikisCmd = new NpgsqlCommand(cikisQuery, connection))
                            {
                                cikisCmd.Parameters.AddWithValue("@uye_id", uyeId);
                                cikisCmd.ExecuteNonQuery();
                            }

                            // Aktif ki�i say�s�n� g�ncelle
                            AktifKisiSayisiGuncelle();

                            MessageBox.Show("��k�� i�lemi ba�ar�l�.");
                        }
                    }
                    else
                    {
                        MessageBox.Show("�ye bulunamad�.");
                    }
                    reader.Close();  // Reader kapat�lmal�
                }
            }
            ListeleUyeler(); // �yeleri yeniden listele
            txtTcCikis.Clear();
        }










        // Paketlerin listelenmesi
        private void ListelePaketler()
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT paket_id, paket_adi, fiyat, giris_limiti FROM \"Uyelik_Paketleri\""; // 3 �zellik eklenmi�tir
                using (var cmd = new NpgsqlCommand(query, connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        DataTable dt = new DataTable();
                        dt.Load(reader);
                        dgvPaketler.DataSource = dt; // Paketlerin listelenece�i DataGridView
                    }
                }
            }
        }

        // Paket ekleme butonuna t�klan�nca �al��acak fonksiyon
        private void btnPaketEkle_Click(object sender, EventArgs e)
        {
            // Bo� alan kontrol�
            if (string.IsNullOrWhiteSpace(txtPaketAdi.Text) || string.IsNullOrWhiteSpace(txtPaketFiyat.Text) || string.IsNullOrWhiteSpace(txtGirisLimiti.Text))
            {
                MessageBox.Show("L�tfen t�m alanlar� doldurun.");
                return;
            }

            // Fiyat ve giri� limiti i�in t�r kontrol�
            decimal fiyat;
            if (!decimal.TryParse(txtPaketFiyat.Text, out fiyat))
            {
                MessageBox.Show("L�tfen ge�erli bir fiyat girin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int girisLimiti;
            if (!int.TryParse(txtGirisLimiti.Text, out girisLimiti))
            {
                MessageBox.Show("L�tfen ge�erli bir giri� limiti girin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string paketAdi = txtPaketAdi.Text;

            // Veritaban�na ba�lanarak paket ekleme i�lemi
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

                MessageBox.Show("Paket ba�ar�yla eklendi.");
                ListelePaketler(); // Paketler tekrar listelensin
            }

            // Y�klemeler ve temizlemeler
            PaketleriYukle();
            LoadPaketler();
            txtPaketAdi.Clear();
            txtPaketFiyat.Clear();
            txtGirisLimiti.Clear();
        }


        // Paket silme butonuna t�klan�nca �al��acak fonksiyon
        private void btnPaketSil_Click(object sender, EventArgs e)
        {
            // Paket ID'si bo� ise kullan�c�y� uyar
            if (string.IsNullOrWhiteSpace(txtPaketIdSil.Text))
            {
                MessageBox.Show("L�tfen silmek istedi�iniz paketin ID'sini girin.", "Uyar�", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Paket ID'sini al
            int paketId;
            if (!int.TryParse(txtPaketIdSil.Text, out paketId))
            {
                MessageBox.Show("L�tfen ge�erli bir paket ID'si girin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Veritaban�na ba�lanarak SQL fonksiyonunu �a��r
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                // SQL fonksiyonunu �a��rma
                string query = "SELECT sil_paket(@paket_id)";
                using (var cmd = new NpgsqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@paket_id", paketId);

                    try
                    {
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Paket ba�ar�yla silindi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ListelePaketler(); // Paketler tekrar listelensin
                    }
                    catch (PostgresException ex)
                    {
                        // Hata mesaj�n� kullan�c�ya g�ster
                        MessageBox.Show($"Paket silme s�ras�nda hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

            // Silme sonras� temizleme
            txtPaketIdSil.Clear();
        }




        // Verileri y�kleme fonksiyonu
        private void VerileriYukle()
        {
            try
            {
                // DataGridView'i temizle
                dgvSporAletleri.Rows.Clear();
                dgvSporAletleri.Columns.Clear(); // Mevcut s�tunlar� temizle

                // S�tunlar� olu�tur
                dgvSporAletleri.Columns.Add("aletID", "Alet ID");
                dgvSporAletleri.Columns.Add("aletAdi", "Alet Ad�");
                dgvSporAletleri.Columns.Add("aletTuru", "Alet T�r�");
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

                                // DataGridView'e sat�r ekle
                                dgvSporAletleri.Rows.Add(aletID, aletAdi, aletTuru, fiyat.ToString("C"), miktar);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Veri Y�kleme Hatas�", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        // Spor aleti ekleme fonksiyonu
        private void btnAletEkle_Click(object sender, EventArgs e)
        {
            string aletAdi = txtAletAdi.Text;
            string aletTuru = txtAletTuru.Text;
            decimal fiyat;
            int miktar;

            // Fiyat ve Miktar do�rulamas�
            if (string.IsNullOrWhiteSpace(aletAdi) || string.IsNullOrWhiteSpace(aletTuru))
            {
                MessageBox.Show("L�tfen alet ad� ve t�r�n� giriniz.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtFiyat.Text, out fiyat) || fiyat <= 0)
            {
                MessageBox.Show("Ge�erli bir fiyat giriniz.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(txtMiktar.Text, out miktar) || miktar < 0)
            {
                MessageBox.Show("Ge�erli bir miktar giriniz.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

                        // Yeni alet ID'sini al�yoruz
                        int yeniAletID = (int)cmd.ExecuteScalar();

                        // Spor_Aletleri'ne ekleme
                        string querySpor = "INSERT INTO \"Spor_Aletleri\" (alet_id, miktar) VALUES (@alet_id, @miktar);";
                        using (NpgsqlCommand cmdSpor = new NpgsqlCommand(querySpor, conn))
                        {
                            cmdSpor.Parameters.AddWithValue("@alet_id", yeniAletID);
                            cmdSpor.Parameters.AddWithValue("@miktar", miktar);
                            cmdSpor.ExecuteNonQuery();
                        }

                        MessageBox.Show("Spor aleti ba�ar�yla eklendi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        VerileriYukle(); // DataGridView yenileme
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Alanlar� temizle
            txtAletAdi.Clear();
            txtAletTuru.Clear();
            txtFiyat.Clear();
            txtMiktar.Clear();
        }

        // Spor aleti silme fonksiyonu
        private void btnAletSil_Click(object sender, EventArgs e)
        {
            // Alet ID'sinin ge�erli olup olmad���n� kontrol et
            if (!int.TryParse(txtAletID.Text, out int aletID) || aletID <= 0)
            {
                MessageBox.Show("L�tfen ge�erli bir Alet ID giriniz!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                                    $"Alet Ad�: {aletAdi}\nAlet T�r�: {aletTuru}\nFiyat: {fiyat:C}\nBu aleti silmek istedi�inizden emin misiniz?",
                                    "Alet Silme Onay�",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Question
                                );

                                if (dialogResult == DialogResult.Yes)
                                {
                                    // Alet silme i�lemi
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
                                                MessageBox.Show("Spor aleti ba�ar�yla silindi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                                VerileriYukle(); // DataGridView yenileme
                                            }
                                            else
                                            {
                                                MessageBox.Show("Alet silinirken bir hata olu�tu.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                MessageBox.Show("Belirtilen ID'ye sahip bir spor aleti bulunamad�.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Alet ID alan�n� temizle
            txtAletID.Clear();
        }


        private void LoadPaketler()
        {
            // ComboBox'� temizle
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

            // ComboBox'ta se�ilen ��enin ne olaca��n� belirlemek i�in ToString metodu
            cbPaketSecimi1.DisplayMember = "PaketAdi";
            cbPaketSecimi1.ValueMember = "PaketId";

            // �lk item se�ili yapmak (iste�e ba�l�)
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

                // D�zeltilmi� sorgu: Uyeler ve Insan tablolar� ile birlikte Uye_Odemeleri tablosu birle�tiriliyor.
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
                        // DataTable olu�turun ve verileri ekleyin
                        DataTable dt = new DataTable();
                        dt.Load(reader);

                        // DataGridView'e veri ba�lama
                        dgvOdeme.DataSource = dt;
                    }
                }
            }
        }

        // Paket se�imi de�i�ti�inde fiyat ve giri� limiti etiketlerini g�ncelle
        private void cbPaketSecimi1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbPaketSecimi1.SelectedIndex != -1)
            {
                // Se�ilen paket ID'sini al
                int paketId = ((dynamic)cbPaketSecimi1.SelectedItem).PaketId;

                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    // Se�ilen paket i�in fiyat ve giri� limitini al
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

                                // Fiyat ve giri� limitini etiketlere yaz
                                lblPaketFiyat1.Text = $"Fiyat: {fiyat:C}";
                                lblGirisLimit1.Text = $"Giri� Limiti: {girisLimit}";
                            }
                        }
                    }
                }
            }
            else
            {
                // Paket se�ilmediyse etiketleri temizle
                lblPaketFiyat1.Text = "Fiyat: -";
                lblGirisLimit1.Text = "Giri� Limiti: -";
            }
        }

        // �deme yapma butonuna t�kland���nda �al��acak metod
        private void btnOdemeYap_Click(object sender, EventArgs e)
        {
            string tcNumarasi = txtTcNumarasi.Text.Trim();

            // TC numaras�n�n 11 haneli ve rakamlardan olu�up olu�mad���n� kontrol et
            if (tcNumarasi.Length != 11 || !tcNumarasi.All(char.IsDigit))
            {
                MessageBox.Show("TC numaras� 11 haneli ve sadece rakamlardan olu�mal�d�r.");
                return;
            }

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                // TC numaras�na ait �yenin olup olmad���n� kontrol et
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
                        MessageBox.Show("Bu TC numaras�na sahip bir �ye bulunamad�.");
                        return;
                    }
                }

                // Paket se�imini al
                if (cbPaketSecimi1.SelectedIndex == -1)
                {
                    MessageBox.Show("L�tfen bir paket se�in.");
                    return;
                }

                int paketId = ((dynamic)cbPaketSecimi1.SelectedItem).PaketId;

                // Se�ilen paketin fiyat�n� al
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

                // �ye bilgilerini al
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

                // �deme kayd�n� olu�tur
                string odemeQuery = "INSERT INTO \"Uye_Odemeleri\" (uye_id, tutar, paket_id) VALUES (@uye_id, @tutar, @paket_id)";
                using (var cmd = new NpgsqlCommand(odemeQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@uye_id", uyeId);
                    cmd.Parameters.AddWithValue("@tutar", fiyat);
                    cmd.Parameters.AddWithValue("@paket_id", paketId);
                    cmd.ExecuteNonQuery();
                }

                // DataGridView'i g�ncelle
                LoadOdemeData();
                ListeleUyeler();
                txtTcNumarasi.Clear();
                cbPaketSecimi1.SelectedIndex = -1;
                MessageBox.Show("�deme ba�ar�yla tamamland�.");
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
        a.aktif_baslangic AS ""Giri� Saati"", 
        a.aktif_bitis AS ""��k�� Saati"",
        EXTRACT(EPOCH FROM (COALESCE(a.aktif_bitis, NOW()) - a.aktif_baslangic)) / 60 AS ""Kullan�m S�resi (Dakika)""
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
                // Aktif ki�i say�s�n�n son kayd�n� al�yoruz
                string query = @"
            SELECT aktif_sayisi 
            FROM ""Aktif_Kisi_Sayisi""
            ORDER BY tarih DESC
            LIMIT 1";  // Sadece son kayd� al�yoruz (en son aktif ki�i say�s�)

                NpgsqlCommand command = new NpgsqlCommand(query, connection);

                try
                {
                    connection.Open();

                    // Aktif ki�i say�s�n� al�yoruz
                    object result = command.ExecuteScalar();
                    int aktifKisiSayisi = result != DBNull.Value ? Convert.ToInt32(result) : 0;

                    // Aktif ki�i say�s�n� etiket �zerinde g�steriyoruz
                    //lblAktifKisiSayisi.Text = $"�u anda i�eride: {aktifKisiSayisi} ki�i";
                }
                catch (Exception ex)
                {
                    // Hata durumunda loglama yapabiliriz veya kullan�c�y� bilgilendirebiliriz
                    Console.WriteLine($"Hata: {ex.Message}");
                }
            }
        }











        private void btnVerileriYenile_Click(object sender, EventArgs e)
        {
            AktifUyeleriGetir();
            AktifKisiSayisiGuncelle();
        }


        // Puanlar� listele butonu i�in kod
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
                    g.kalan_giris AS ""Giri� Hakk�""
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
                        MessageBox.Show("Veri bulunamad�.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata olu�tu: {ex.Message}");
            }
        }

        private void btnAra1_Click(object sender, EventArgs e)
        {
            string tc = textBoxTc.Text.Trim();

            if (tc.Length != 11 || !long.TryParse(tc, out _))
            {
                MessageBox.Show("L�tfen ge�erli bir 11 haneli TC kimlik numaras� giriniz!");
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
                    g.kalan_giris AS ""Giri� Hakk�""
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
                        MessageBox.Show("Girilen TC kimlik numaras�na ait �ye bulunamad�.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata olu�tu: {ex.Message}");
            }
        }



        private void btnAraGuncelle_Click(object sender, EventArgs e)
        {
            // TC numaras�n�n bo� olmad���n� kontrol et
            if (string.IsNullOrWhiteSpace(txtTcGuncelle.Text))
            {
                MessageBox.Show("L�tfen TC numaras�n� girin.");
                return;
            }

            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL fonksiyonunu �a��rarak veriyi al�yoruz
                    using (var cmd = new NpgsqlCommand("SELECT * FROM get_insan_bilgileri(@tc_numarasi)", connection))
                    {
                        cmd.Parameters.AddWithValue("@tc_numarasi", txtTcGuncelle.Text);
                        var reader = cmd.ExecuteReader();
                        if (reader.Read())
                        {
                            // Ki�inin ad� soyad�, do�um tarihi, telefon ve email bilgilerini formda g�ster
                            lblKisiAdiSoyadi.Text = $"{reader["ad"]} {reader["soyad"]}";
                            txtAdGuncelle.Text = reader["ad"].ToString();
                            txtSoyadGuncelle.Text = reader["soyad"].ToString();
                            dtpDogumTarihiGuncelle.Value = Convert.ToDateTime(reader["dogum_tarihi"]);
                            txtTelGuncelle.Text = reader["telefon"].ToString();
                            txtEmailGuncelle.Text = reader["email"].ToString();
                        }
                        else
                        {
                            MessageBox.Show("Girilen TC numaras�na ait �ye bulunamad�.");
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
            // Gerekli alanlar�n dolduruldu�unu kontrol et
            if (string.IsNullOrWhiteSpace(txtAdGuncelle.Text) ||
                string.IsNullOrWhiteSpace(txtSoyadGuncelle.Text) ||
                string.IsNullOrWhiteSpace(txtTelGuncelle.Text) ||
                string.IsNullOrWhiteSpace(txtEmailGuncelle.Text))
            {
                MessageBox.Show("L�tfen t�m bilgileri doldurun.");
                return;
            }

            // Telefon numaras�n�n format�n� kontrol et
            if (!System.Text.RegularExpressions.Regex.IsMatch(txtTelGuncelle.Text, @"^\d{10}$"))
            {
                MessageBox.Show("Ge�erli bir telefon numaras� girin.");
                return;
            }

            // E-posta adresinin format�n� kontrol et
            try
            {
                var mailAddress = new System.Net.Mail.MailAddress(txtEmailGuncelle.Text);
            }
            catch (FormatException)
            {
                MessageBox.Show("Ge�erli bir e-posta adresi girin.");
                return;
            }

            // Ya� kontrol� (18 ya��ndan b�y�k olmal�)
            DateTime dogumTarihi = dtpDogumTarihiGuncelle.Value;
            int yas = DateTime.Now.Year - dogumTarihi.Year;
            if (DateTime.Now.DayOfYear < dogumTarihi.DayOfYear)
                yas--;

            if (yas < 18)
            {
                MessageBox.Show("�ye 18 ya��ndan k���k olamaz.");
                return;
            }

            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    // TC numaras�na g�re insan_id'yi al
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
                            MessageBox.Show("Girilen TC numaras�na ait �ye bulunamad�.");
                            return;
                        }
                    }

                    // "Insan" tablosundaki ki�isel bilgileri g�ncelle
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

                    // G�ncelleme ba�ar�l� oldu�unda kullan�c�ya bilgi ver
                    MessageBox.Show("�ye ba�ar�yla g�ncellendi.");
                    lblKisiAdiSoyadi.Text = "G�ncelleme ba�ar�l�.";

                    // Formu temizle
                    txtTcGuncelle.Clear();
                    txtAdGuncelle.Clear();
                    txtSoyadGuncelle.Clear();
                    txtTelGuncelle.Clear();
                    txtEmailGuncelle.Clear();
                    dtpDogumTarihiGuncelle.Value = DateTime.Now;

                    // Label'� temizle
                    lblKisiAdiSoyadi.Text = "";
                    ListeleUyeler();
                }
            }
            catch (PostgresException ex)
            {
                MessageBox.Show("Hata: " + ex.Message);
            }
        }



        // Bilgilendirmeleri veritaban�ndan y�kle
        private void LoadBilgilendirmeler()
        {
            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open(); // Ba�lant�y� a��yoruz
                    string query = "SELECT * FROM \"Bilgilendirme\""; // Tablo ad�n� do�ru yazd�k
                    NpgsqlDataAdapter dataAdapter = new NpgsqlDataAdapter(query, connection);
                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable); // Verileri dolduruyoruz
                    dataGridViewBilgilendirme.DataSource = dataTable; // DataGridView'e ba�l�yoruz
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata olu�tu: {ex.Message}"); // Hata mesaj�n� g�ster
            }
        }



        // Yeni bilgilendirme ekleme
        private void btnEkle_Click(object sender, EventArgs e)
        {
            string baslik = txtBaslik.Text.Trim();
            string aciklama = txtAciklama.Text.Trim();
            DateTime gecerlilikTarihi = dateTimePickerGecerlilik.Value;

            // Ba�l�k ve a��klama bo� olup olmad���n� kontrol et
            if (string.IsNullOrEmpty(baslik) || string.IsNullOrEmpty(aciklama))
            {
                MessageBox.Show("Ba�l�k ve a��klama bo� olamaz.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                LoadBilgilendirmeler(); // Listeyi g�ncelle
                MessageBox.Show("Bilgilendirme ba�ar�yla eklendi.", "Ba�ar�l�", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata olu�tu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        // Se�ilen bilgilendirmeyi silme
        private void btnSil_Click(object sender, EventArgs e)
        {
            string bilgilendirmeIdText = txtBilgilendirmeId.Text.Trim();

            // ID bo� mu kontrol et
            if (string.IsNullOrEmpty(bilgilendirmeIdText))
            {
                MessageBox.Show("Silmek i�in ID girin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // ID ge�erli mi kontrol et
            if (!int.TryParse(bilgilendirmeIdText, out int bilgilendirmeId))
            {
                MessageBox.Show("Ge�erli bir ID girin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL fonksiyonunu �a��r
                    string query = "SELECT sil_bilgilendirme(@BilgilendirmeId)";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@BilgilendirmeId", bilgilendirmeId);
                        command.ExecuteNonQuery();
                    }
                }

                LoadBilgilendirmeler(); // Listeyi g�ncelle
                MessageBox.Show("Bilgilendirme ba�ar�yla silindi.", "Ba�ar�l�", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata olu�tu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Form2 ve Form3'� kontrol edip kapat�yoruz
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


