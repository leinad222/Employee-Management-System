using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace Employee_Management_System
{
    public class FileEmployeeRepository : IEmployeeRepository
    {
        private readonly string _path;

        public FileEmployeeRepository()
        {
            _path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "employees.xml");
        }

        public List<Employee> LoadAll()
        {
            if (!File.Exists(_path))
                return new List<Employee>();

            try
            {
                var serializer = new XmlSerializer(typeof(List<Employee>));
                using (var stream = File.OpenRead(_path))
                {
                    return (List<Employee>)serializer.Deserialize(stream) ?? new List<Employee>();
                }
            }
            catch
            {
                return new List<Employee>();
            }
        }

        public void Save(Employee employee)
        {
            var list = LoadAll();
            var existing = list.Find(e => string.Equals(e.Id, employee.Id, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                // update
                existing.Name = employee.Name;
                existing.Designation = employee.Designation;
                existing.BasicPay = employee.BasicPay;
                existing.Conveyance = employee.Conveyance;
                existing.Medical = employee.Medical;
                existing.HouseRent = employee.HouseRent;
                existing.GrossPay = employee.GrossPay;
                existing.IncomeTax = employee.IncomeTax;
                existing.NetSalary = employee.NetSalary;
            }
            else
            {
                // add
                list.Add(employee);
            }

            Persist(list);
        }

        public void Delete(string id)
        {
            var list = LoadAll();
            list.RemoveAll(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));
            Persist(list);
        }

        private void Persist(List<Employee> list)
        {
            var serializer = new XmlSerializer(typeof(List<Employee>));
            using (var stream = File.Open(_path, FileMode.Create))
            {
                serializer.Serialize(stream, list);
            }
        }
    }
}