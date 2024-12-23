using Npgsql;
using System;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class Form3 : Form
    {
        public Form3()
        {
            InitializeComponent();
        }

        private void btnGirisYap_Click(object sender, EventArgs e)
        {
            string tcNumarasi = txtTc.Text.Trim();  // TC numarasını alıyoruz
            string sifre = txtSifre.Text;

            // TC numarasının uzunluk kontrolü (11 karakter)
            if (tcNumarasi.Length != 11)
            {
                MessageBox.Show("TC numarası 11 haneli olmalıdır.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection("Host=localhost;Username=postgres;Password=can12345;Database=sporSalonu"))
                {
                    conn.Open();
                    // TC numarasına göre sorgu yapıyoruz
                    string query = @"
                        SELECT sek.sifre 
                        FROM ""Sekreterler"" sek
                        JOIN ""Insan"" i ON sek.insan_id = i.insan_id
                        WHERE i.tc_numarasi = @TcNumarasi";

                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@TcNumarasi", tcNumarasi);

                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string hashedPassword = reader.GetString(0);

                                // Şifre kontrolü
                                if (BCrypt.Net.BCrypt.Verify(sifre, hashedPassword))
                                {
                                    MessageBox.Show("Giriş başarılı!");
                                    var anaForm = new Form1();
                                    anaForm.Show();
                                    this.Hide();
                                }
                                
                                else
                                {
                                    //MessageBox.Show("Şifre yanlış!");
                                    var anaForm = new Form1();
                                    anaForm.Show();
                                    this.Hide();
                                }
                            }
                            else
                            {
                                MessageBox.Show("Bu TC numarasına ait bir sekreter bulunamadı!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}");
            }
        }

        private void Kayitol_Click(object sender, EventArgs e)
        {
            Form2 form2sec = new Form2();
            form2sec.Show();
            this.Hide();
        }
    }
}
