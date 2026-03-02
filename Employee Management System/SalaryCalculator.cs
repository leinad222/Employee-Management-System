namespace Employee_Management_System
{
    public static class SalaryCalculator
    {
        // Returns a tuple with computed salary components.
        public static (decimal Conveyance, decimal Medical, decimal HouseRent, decimal GrossPay, decimal IncomeTax, decimal NetSalary) Calculate(decimal basicPay)
        {
            if (basicPay < 0) basicPay = 0;

            decimal CA = basicPay * 0.04m; // 4%
            decimal MA = basicPay * 0.05m; // 5%
            decimal HR = basicPay * 0.10m; // 10%

            decimal gross = basicPay + CA + MA + HR;
            decimal taxRate = gross > 6000m ? 0.15m : 0.10m;
            decimal incomeTax = gross * taxRate;
            decimal net = gross - incomeTax;

            return (CA, MA, HR, gross, incomeTax, net);
        }
    }
}