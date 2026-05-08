using CogniCareer.Data;
using CogniCareer.Models;

namespace CogniCareer.Services
{
    public class CompanyService
    {
        private readonly CompanyData _companyData = new();

        public Company? GetByUserID(int userID) => _companyData.GetByUserID(userID);
        public List<Company> GetPending() => _companyData.GetPendingCompanies();
        public List<Company> GetAll() => _companyData.GetAllCompanies();
        public bool Approve(int companyID) => _companyData.ApproveCompany(companyID);
        public bool Reject(int companyID) => _companyData.RejectCompany(companyID);
        public bool Update(Company c) => _companyData.UpdateCompany(c);
    }
}