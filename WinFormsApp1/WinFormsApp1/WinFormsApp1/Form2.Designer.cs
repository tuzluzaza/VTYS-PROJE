namespace WinFormsApp1
{
    partial class Form2
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            txtAd = new TextBox();
            txtSoyad = new TextBox();
            txtTc = new TextBox();
            txtSifre = new TextBox();
            txtSifreOnay = new TextBox();
            btnKayitOl = new Button();
            label2 = new Label();
            label1 = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            label6 = new Label();
            SuspendLayout();
            // 
            // txtAd
            // 
            txtAd.Location = new Point(233, 99);
            txtAd.Name = "txtAd";
            txtAd.Size = new Size(141, 27);
            txtAd.TabIndex = 0;
            // 
            // txtSoyad
            // 
            txtSoyad.Location = new Point(233, 145);
            txtSoyad.Name = "txtSoyad";
            txtSoyad.Size = new Size(141, 27);
            txtSoyad.TabIndex = 1;
            // 
            // txtTc
            // 
            txtTc.Location = new Point(233, 193);
            txtTc.Name = "txtTc";
            txtTc.Size = new Size(141, 27);
            txtTc.TabIndex = 2;
            // 
            // txtSifre
            // 
            txtSifre.Location = new Point(233, 244);
            txtSifre.Name = "txtSifre";
            txtSifre.PasswordChar = '*';
            txtSifre.Size = new Size(141, 27);
            txtSifre.TabIndex = 3;
            // 
            // txtSifreOnay
            // 
            txtSifreOnay.Location = new Point(233, 288);
            txtSifreOnay.Name = "txtSifreOnay";
            txtSifreOnay.PasswordChar = '*';
            txtSifreOnay.Size = new Size(141, 27);
            txtSifreOnay.TabIndex = 4;
            // 
            // btnKayitOl
            // 
            btnKayitOl.Font = new Font("Tw Cen MT", 12F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point);
            btnKayitOl.Location = new Point(200, 358);
            btnKayitOl.Name = "btnKayitOl";
            btnKayitOl.Size = new Size(122, 43);
            btnKayitOl.TabIndex = 5;
            btnKayitOl.Text = "Kayıt Ol";
            btnKayitOl.UseVisualStyleBackColor = true;
            btnKayitOl.Click += btnKayitOl_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Trebuchet MS", 12F, FontStyle.Bold, GraphicsUnit.Point);
            label2.Location = new Point(124, 145);
            label2.Name = "label2";
            label2.Size = new Size(92, 26);
            label2.TabIndex = 7;
            label2.Text = "SOYAD : ";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Trebuchet MS", 12F, FontStyle.Bold, GraphicsUnit.Point);
            label1.Location = new Point(159, 100);
            label1.Name = "label1";
            label1.Size = new Size(57, 26);
            label1.TabIndex = 6;
            label1.Text = "AD : ";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Trebuchet MS", 12F, FontStyle.Bold, GraphicsUnit.Point);
            label3.Location = new Point(141, 245);
            label3.Name = "label3";
            label3.Size = new Size(75, 26);
            label3.TabIndex = 9;
            label3.Text = "Şİfre : ";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Trebuchet MS", 12F, FontStyle.Bold, GraphicsUnit.Point);
            label4.Location = new Point(128, 194);
            label4.Name = "label4";
            label4.Size = new Size(88, 26);
            label4.TabIndex = 8;
            label4.Text = "TC NO : ";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Trebuchet MS", 12F, FontStyle.Bold, GraphicsUnit.Point);
            label5.Location = new Point(68, 291);
            label5.Name = "label5";
            label5.Size = new Size(148, 26);
            label5.TabIndex = 11;
            label5.Text = "Şİfre Tekrarı : ";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Verdana Pro Semibold", 16.2F, FontStyle.Bold, GraphicsUnit.Point);
            label6.Location = new Point(184, 29);
            label6.Name = "label6";
            label6.Size = new Size(158, 34);
            label6.TabIndex = 12;
            label6.Text = "KAYIT OL";
            // 
            // Form2
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(525, 486);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(label3);
            Controls.Add(label4);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(btnKayitOl);
            Controls.Add(txtSifreOnay);
            Controls.Add(txtSifre);
            Controls.Add(txtTc);
            Controls.Add(txtSoyad);
            Controls.Add(txtAd);
            Name = "Form2";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Form2";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtAd;
        private TextBox txtSoyad;
        private TextBox txtTc;
        private TextBox txtSifre;
        private TextBox txtSifreOnay;
        private Button btnKayitOl;
        private Label label2;
        private Label label1;
        private Label label3;
        private Label label4;
        private Label label5;
        private Label label6;
    }
}