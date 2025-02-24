namespace AwesomeGICBank.ConsoleApp.Dtos
{
    public class StatementRequestDto
    {
        public string? AccountId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }

        public bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(AccountId))
            {
                errorMessage = "AccountId cannot be empty.";
                return false;
            }

            if (!(Year >= 1900 && Year <= DateTime.Now.Year))
            {
                errorMessage = "Invalid Year";
                return false;
            }

            if (!(Month >= 1 && Month <= 12))
            {
                errorMessage = "Invalid Month";
                return false;
            }

            return true;
        }
    }
}