using Microsoft.Data.SqlClient;
using CogniCareer.Models;
using System.Data;

namespace CogniCareer.Data
{
    public class JobSkillData
    {
        public List<JobSkill> GetByJobID(int jobID)
        {
            var list = new List<JobSkill>();
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_GetSkillsByJob", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@JobID", jobID);
                con.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new JobSkill
                    {
                        JobSkillID = Convert.ToInt32(reader["JobSkillID"]),
                        JobID = Convert.ToInt32(reader["JobID"]),
                        SkillID = Convert.ToInt32(reader["SkillID"]),
                        SkillName = reader["SkillName"].ToString() ?? "",
                        Priority = reader["Priority"].ToString() ?? ""
                    });
                }
            }
            catch { }
            return list;
        }

        public bool AddJobSkill(JobSkill js)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_AddJobSkill", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@JobID", js.JobID);
                cmd.Parameters.AddWithValue("@SkillID", js.SkillID);
                cmd.Parameters.AddWithValue("@Priority", js.Priority);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch { return false; }
        }

        public bool DeleteByJobID(int jobID)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_DeleteJobSkillsByJob", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@JobID", jobID);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch { return false; }
        }
    }
}