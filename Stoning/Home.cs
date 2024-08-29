using DataAccessLayer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Stoning
{
    public partial class Home : Form
    {
        public static Employee LoginUser;
        DataModel dm = new DataModel();
        int QualityID = 0;
        int FaultID = 0;
        public Home()
        {
            InitializeComponent();
        }

        private void Home_Load(object sender, EventArgs e)
        {
            LoginPage frm = new LoginPage();

            if (frm.ShowDialog() == DialogResult.OK)
            {
                // Assume Helpers.isLogin is a static property holding the logged-in user's info
                Employee model = Helpers.isLogin;
                Home.LoginUser.ID = model.ID; // Assuming QualityPersonalID is obtained from the logged-in user
            }
            // ComboBox'a verileri ekliyoruz
            cb_result.ValueMember = "ID";
            cb_result.DisplayMember = "Name";
            cb_result.DataSource = dm.GetResult();

            loadGrid();
        }

        private void loadGrid()
        {
            var result = dm.logEntryListStoning(new DataAccessLayer.Stoning
            {
                Barcode = tb_barcode.Text,
                ResultID = cb_result.SelectedIndex,
            });

            if (result != null)
            {
                var rt = result.OrderByDescending(r => r.ID).ToList();
                DataTable dt = new DataTable();

                dt.Columns.Add("ID");
                dt.Columns.Add("Barkod No");
                dt.Columns.Add("Kalite");
                dt.Columns.Add("Sonuç");
                dt.Columns.Add("Kontrol Tarihi");
                dt.Columns.Add("Taşlama Personeli");

                foreach (var item in rt)
                {
                    DataRow r = dt.NewRow();
                    r["ID"] = item.ID;
                    r["Barkod No"] = item.Barcode;
                    r["Kalite"] = item.Quality;
                    r["Sonuç"] = item.Result;
                    r["Kontrol Tarihi"] = item.DateTime.ToShortDateString();
                    r["Taşlama Personeli"] = item.QualityPersonal;
                    dt.Rows.Add(r);
                }

                dgv_Stoning.DataSource = dt;
                // Yalnızca veri içeren satırları say
                int nonEmptyRowCount = dgv_Stoning.Rows.Cast<DataGridViewRow>()
                    .Count(row => !row.IsNewRow && row.Cells.Cast<DataGridViewCell>().Any(cell => cell.Value != null && cell.Value.ToString() != ""));

                lbl_number.Text = "Bakılan Ürün sayısı: " + nonEmptyRowCount;

            }
            else
            {
                MessageBox.Show("Veri yüklenirken bir hata oluştu.");
            }
            tb_barcode.Select();
        }


        private void tb_barcode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)

            {
                if (tb_barcode.Text.Length == 10)
                {
                    DataAccessLayer.Stoning stoning = new DataAccessLayer.Stoning();
                    if (dm.isThereResult(Convert.ToInt32(cb_result.SelectedIndex)))
                    {
                        stoning.Barcode = tb_barcode.Text;
                        stoning.QualityID = cb_fire.Checked ? 5 : dm.getBarcodeQuality(tb_barcode.Text).FirstOrDefault()?.QualityID ?? 0;
                        if (stoning.QualityID == 5)
                        {
                            dm.updateProductQuality(stoning);
                        }
                        stoning.ResultID = cb_result.SelectedIndex;
                        stoning.DateTime = DateTime.Now;
                        stoning.QualityPersonalID = Home.LoginUser.ID;

                        if (dm.createVacuumTest(stoning))
                        {
                            tb_barcode.Text = "";
                            cb_fire.Checked = false;
                            loadGrid(); // Refresh the grid after saving
                        }
                        else
                        {
                            MessageBox.Show("Taşlama testi kaydedilemedi.");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Geçersiz Barkod Numarası.");
                        tb_barcode.Text = "";
                    }
                }
                else
                {
                    MessageBox.Show("Sonuç girilmedi.");
                }
            }
        }
    }
}
