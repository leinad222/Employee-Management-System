namespace Employee_Management_System
{
    public class Employee
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Designation { get; set; }
        public decimal BasicPay { get; set; }
        public decimal Conveyance { get; set; }
        public decimal Medical { get; set; }
        public decimal HouseRent { get; set; }
        public decimal GrossPay { get; set; }
        public decimal IncomeTax { get; set; }
        public decimal NetSalary { get; set; }
    }
}