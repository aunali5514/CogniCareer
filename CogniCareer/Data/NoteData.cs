using Microsoft.Data.SqlClient;
using CogniCareer.Models;
using System.Data;

namespace CogniCareer.Data
{
    public class NoteData
    {
        public bool AddNote(ApplicationNote note)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_AddApplicationNote", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ApplicationID", note.ApplicationID);
                cmd.Parameters.AddWithValue("@NoteText", note.NoteText);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch { return false; }
        }

        public List<ApplicationNote> GetByApplication(int applicationID)
        {
            var list = new List<ApplicationNote>();
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_GetApplicationNotes", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ApplicationID", applicationID);
                con.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new ApplicationNote
                    {
                        NoteID = Convert.ToInt32(reader["NoteID"]),
                        ApplicationID = Convert.ToInt32(reader["ApplicationID"]),
                        NoteText = reader["NoteText"].ToString() ?? "",
                        CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                    });
                }
            }
            catch { }
            return list;
        }
    }
}