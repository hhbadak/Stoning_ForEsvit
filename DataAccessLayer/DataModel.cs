using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer
{
    public class DataModel
    {
        private SqlConnection con;
        private SqlCommand cmd;
        private readonly string _connectionString = ConnectionStrings.ConStr; // ConnectionStrings sınıfındaki bağlantı dizesini kullanın.

        public DataModel()
        {
            con = new SqlConnection(_connectionString);
            cmd = con.CreateCommand();
        }

        #region Personal Metot
        public Employee personalLogin(string username, string password)
        {
            Employee model = new Employee();
            try
            {
                cmd.CommandText = "SELECT Kimlik FROM kullanici_liste WHERE kullanici_adi = @uName AND sifre = @password";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@uName", username);
                cmd.Parameters.AddWithValue("@password", password);
                con.Open();
                int id = Convert.ToInt32(cmd.ExecuteScalar());
                if (id > 0)
                {
                    model = getPersonal(id);
                }
                return model;

            }
            catch
            {
                return null;
            }
            finally { con.Close(); }
        }

        public Employee getPersonal(int id)
        {
            try
            {
                Employee model = new Employee();
                cmd.CommandText = "SELECT Kimlik, kullanici_adi, sifre, ad_soyad, durum, pcAd, versiyon, KisaAd, Departman \r\nFROM kullanici_liste\r\nWHERE Kimlik = @id";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@id", id);
                if (con.State != System.Data.ConnectionState.Open)
                {
                    con.Open();
                }
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    model.ID = Convert.ToInt32(reader["Kimlik"]);
                    model.Username = reader.GetString(1);
                    model.Password = reader.GetString(2);
                    model.NameSurname = reader.GetString(3);
                    model.Status = reader.GetByte(4);
                    model.PcName = reader.GetString(5);
                    model.Version = reader.GetString(6);
                    model.ShortName = reader.GetString(7);
                    model.Department = reader.GetString(8);
                }
                return model;
            }
            catch
            {
                return null;
            }
            finally { con.Close(); }
        }
        #endregion

        public List<Products> getBarcodeQuality(string barcode)
        {
            List<Products> pr = new List<Products>();
            try
            {
                cmd.CommandText = "SELECT ID, Quality FROM Products WHERE Barcode = @barcode";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@barcode", barcode);
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Products model = new Products
                        {
                            ID = Convert.ToInt32(reader["ID"]),
                            QualityID = Convert.ToInt32(reader["Quality"])
                        };
                        pr.Add(model);
                    }
                }
                return pr;
            }
            catch
            {
                return null;
            }
            finally
            {
                con.Close();
            }
        }

        public bool updateProductAndStoning(DataAccessLayer.Stoning stoning)
        {
            try
            {
                // Önce Products tablosunu güncelle
                cmd.CommandText = "UPDATE Products SET Quality = @quality, Fault = 46 WHERE Barcode = @barcode";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@quality", stoning.QualityID);
                cmd.Parameters.AddWithValue("@barcode", stoning.Barcode);

                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();

                // Ardından Stoning tablosuna kaydı ekle
                cmd.CommandText = "INSERT INTO kalite_STE(Barcode, QualityID, FaultID, DateTime, QualityPersonalID) VALUES(@barcode, @quality, @fault, FORMAT(@date, 'yyyy-MM-dd HH:mm:ss'), @qpid)";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@barcode", stoning.Barcode);
                cmd.Parameters.AddWithValue("@quality", stoning.QualityID);
                cmd.Parameters.AddWithValue("@fault", stoning.ResultID);
                cmd.Parameters.AddWithValue("@date", stoning.DateTime);
                cmd.Parameters.AddWithValue("@qpid", stoning.QualityPersonalID);

                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                con.Close();
            }
        }

        #region Products Metot

        public DataAccessLayer.Stoning getProductDetails(string barcode)
        {
            try
            {
                cmd.CommandText = "SELECT Quality, Fault FROM Products WHERE Barcode = @barcode";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@barcode", barcode);

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                DataAccessLayer.Stoning model = new DataAccessLayer.Stoning();
                if (reader.Read())
                {
                    model.QualityID = reader.GetByte(0);
                    model.ResultID = reader.GetByte(1);
                }
                return model;
            }
            catch
            {
                return null;
            }
            finally
            {
                con.Close();
            }
        }

        public bool updateProductQuality(DataAccessLayer.Stoning stoning)
        {
            try
            {
                cmd.CommandText = "UPDATE Products SET Quality = @quality, Fault = 46 WHERE Barcode=@barcode";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@Quality", stoning.QualityID);
                cmd.Parameters.AddWithValue("@barcode", stoning.Barcode);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch
            {
                return false;
            }
            finally { con.Close(); }
        }

        #endregion

        #region Stoning Metot

        public List<Stoning> logEntryListStoning(Stoning filter)
        {
            List<Stoning> rt = new List<Stoning>();
            try
            {
                cmd.CommandText = @"SELECT kt.ID, kt.Barcode, klt.tanim, rslt.Name, kt.DateTime, kl.ad_soyad  
FROM kalite_Taslama AS kt
JOIN kalite_liste AS klt ON klt.Kimlik = kt.QualityID
JOIN kalite_TaslamaHata AS rslt ON rslt.ID = kt.ResultID
JOIN kullanici_liste AS kl ON kl.Kimlik = kt.QualityPersonalID
WHERE CONVERT(date, kt.DateTime) = @datetime";

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@datetime", DateTime.Now.Date);

                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Stoning model = new Stoning
                        {
                            // Bu satırların veri tiplerini kontrol edin ve doğru dönüşümler yapın
                            ID = reader.GetInt32(0),
                            Barcode = reader.GetString(1),
                            Quality = reader.GetString(2),
                            Result = reader.GetString(3),
                            DateTime = reader.GetDateTime(4),
                            QualityPersonal = reader.GetString(5),
                        };
                        rt.Add(model);
                    }
                }
                return rt;
            }
            catch
            {
                return null;
            }
            finally
            {
                con.Close();
            }
        }

        public bool createVacuumTest(DataAccessLayer.Stoning stoning)
        {
            try
            {
                cmd.CommandText = "INSERT INTO kalite_Taslama(Barcode, QualityID, ResultID, DateTime, QualityPersonalID) VALUES(@barcode, @qid, @result, FORMAT(@date, 'yyyy-MM-dd HH:mm:ss'), @qpid)";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@barcode", stoning.Barcode);
                cmd.Parameters.AddWithValue("@qid", stoning.QualityID);
                cmd.Parameters.AddWithValue("@result", stoning.ResultID);
                cmd.Parameters.AddWithValue("@date", stoning.DateTime);
                cmd.Parameters.AddWithValue("@qpid", stoning.QualityPersonalID);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                con.Close();
            }
        }

        #endregion

        #region Result Metot
        public List<StoningFault> GetResult()
        {
            List<StoningFault> result = new List<StoningFault>();
            try
            {
                cmd.CommandText = "SELECT ID, Name FROM kalite_TaslamaHata";
                cmd.Parameters.Clear();
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    StoningFault sf = new StoningFault() { ID = reader.GetInt32(0), Name = reader.GetString(1) };
                    result.Add(sf);
                }
                return result;
            }
            catch
            {
                return null;
            }
            finally
            {
                con.Close();
            }
        }

        public bool isThereResult(int id)
        {
            try
            {
                cmd.CommandText = "SELECT Id FROM kalite_TaslamaHata WHERE Id = @id";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@id", id);
                con.Open();
                id = Convert.ToInt32(cmd.ExecuteScalar());
                object result = cmd.ExecuteScalar();

                if (result != null)
                {
                    if (result is int)
                    {
                        id = (int)result;
                        return true;
                    }
                    else
                    {
                        // Dönüştürme başarısız oldu
                        // Gerekirse uygun bir hata işleme mekanizması burada eklenebilir
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
            finally { con.Close(); }
        }

        #endregion
    }
}
