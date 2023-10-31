using System.Text;

namespace GPTW.ListAutomation.Core.Infrastructure
{
    public static class Utils
    {
        public static string GenerateRandomCode(int length)
        {
            var result = new StringBuilder();

            for (var i = 0; i < length; i++)
            {
                var r = new Random(Guid.NewGuid().GetHashCode());
                if (i == 0)
                {
                    result.Append(r.Next(1, 10));
                }
                else
                {
                    result.Append(r.Next(0, 10));
                }
            }
            return result.ToString();
        }

        public static string GetIndexRandomNum(int minValue, int maxValue)
        {
            var r = new Random(Guid.NewGuid().GetHashCode());
            var result = r.Next(minValue, maxValue);
            return result.ToString();
        }
    }
}
