using Npgsql;
using System;
using System.Linq;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private void btnKayitOl_Click(object sender, EventArgs e)
        {
            string ad = txtAd.Text;
            string soyad = txtSoyad.Text;
            string tcNumarasi = txtTc.Text;
            string sifre = txtSifre.Text;
            string sifreOnay = txtSifreOnay.Text;

            // TC numarasının uzunluk kontrolü (11 karakter)
            if (tcNumarasi.Length != 11)
            {
                MessageBox.Show("TC numarası 11 haneli olmalıdır!");
                return;
            }

            // Şifre kontrolü
            if (sifre != sifreOnay)
            {
                MessageBox.Show("Şifreler uyuşmuyor!");
                return;
            }
            else
            {
                if (sifre.Length < 8 || !sifre.Any(char.IsDigit) || !sifre.Any(char.IsUpper))
                {
                    MessageBox.Show("Şifre en az 8 karakter uzunluğunda, bir büyük harf ve bir rakam içermelidir!");
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(ad) || string.IsNullOrWhiteSpace(soyad) ||
                string.IsNullOrWhiteSpace(tcNumarasi) || string.IsNullOrWhiteSpace(sifre) ||
                string.IsNullOrWhiteSpace(sifreOnay))
            {
                MessageBox.Show("Tüm Alanları Doldurunuz !");
                return;
            }

            // Şifreyi hashle
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(sifre);

            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection("Host=localhost;Username=postgres;Password=can12345;Database=sporSalonu"))
                {
                    conn.Open();

                    // TC numarası benzersiz mi kontrol et
                    string checkQuery = "SELECT COUNT(*) FROM \"Insan\" WHERE tc_numarasi = @TcNumarasi";
                    using (NpgsqlCommand checkCmd = new NpgsqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@TcNumarasi", tcNumarasi);
                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (count > 0)
                        {
                            MessageBox.Show("Bu TC numarası zaten kayıtlıdır!");
                            return;
                        }
                    }

                    // TC numarası ile insan kaydını oluştur
                    string insertInsanQuery = "INSERT INTO \"Insan\" (ad, soyad, tc_numarasi) VALUES (@ad, @soyad, @tcNumarasi) RETURNING insan_id";
                    int insanId = 0;
                    using (NpgsqlCommand cmd = new NpgsqlCommand(insertInsanQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@ad", ad);
                        cmd.Parameters.AddWithValue("@soyad", soyad);
                        cmd.Parameters.AddWithValue("@tcNumarasi", tcNumarasi);
                        insanId = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // Sekreter kaydını oluştur
                    string insertSekreterQuery = "INSERT INTO \"Sekreterler\" (insan_id, sifre) VALUES (@insanId, @sifre)";
                    using (NpgsqlCommand cmd = new NpgsqlCommand(insertSekreterQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@insanId", insanId);
                        cmd.Parameters.AddWithValue("@sifre", hashedPassword);
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Kayıt başarıyla tamamlandı!");

                // Bilgileri temizle
                txtAd.Clear();
                txtSoyad.Clear();
                txtTc.Clear();
                txtSifre.Clear();
                txtSifreOnay.Clear();

                // Giriş ekranına yönlendir
                Form3 form3 = new Form3();
                form3.Show();
                this.Hide();



            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}");
            }
        }
    }
}
