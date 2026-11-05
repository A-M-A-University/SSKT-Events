using System.ComponentModel.DataAnnotations;

namespace SSKT_Events.Validation
{
    public class NotPastDateAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value == null)
                return true;

            DateTime date = (DateTime)value;

            return date.Date >= DateTime.Today;
        }
    }
}