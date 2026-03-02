using System.Collections.Generic;

namespace Employee_Management_System
{
    public interface IEmployeeRepository
    {
        List<Employee> LoadAll();
        void Save(Employee employee); // add or update by Id
        void Delete(string id);
    }
}