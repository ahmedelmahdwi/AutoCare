using System;
using System.Text;

namespace C__project_Auto_Care.Reports
{
    /// <summary>
    /// Accurate, pure C# Arabic Currency Tafqeet Engine
    /// Converts numeric amounts into Arabic written currency words.
    /// </summary>
    public static class TafqeetHelper
    {
        private static readonly string[] Ones = {
            "", "واحد", "اثنان", "ثلاثة", "أربعة", "خمسة", "ستة", "سبعة", "ثمانية", "تسعة",
            "عشرة", "أحد عشر", "اثنا عشر", "ثلاثة عشر", "أربعة عشر", "خمسة عشر", "ستة عشر",
            "سبعة عشر", "ثمانية عشر", "تسعة عشر"
        };

        private static readonly string[] Tens = {
            "", "عشرة", "عشرون", "ثلاثون", "أربعون", "خمسون", "ستون", "سبعون", "ثمانون", "تسعون"
        };

        private static readonly string[] Hundreds = {
            "", "مائة", "مائتان", "ثلاثمائة", "أربعمائة", "خمسمائة", "ستمائة", "سبعمائة", "ثمانمائة", "تسعمائة"
        };

        public static string ConvertToArabicWords(decimal amount, string currencyName = "دينار", string subunitName = "درهم")
        {
            if (amount == 0) return "فقط صفر " + currencyName + " لا غير";

            long integerPart = (long)Math.Floor(amount);
            int decimalPart = (int)Math.Round((amount - integerPart) * 100);

            var sb = new StringBuilder("فقط ");
            sb.Append(NumberToWords(integerPart));
            sb.Append(" ");
            sb.Append(currencyName);

            if (decimalPart > 0)
            {
                sb.Append(" و ");
                sb.Append(NumberToWords(decimalPart));
                sb.Append(" ");
                sb.Append(subunitName);
            }

            sb.Append(" لا غير");
            return sb.ToString();
        }

        private static string NumberToWords(long number)
        {
            if (number == 0) return "صفر";
            if (number < 0) return "سالب " + NumberToWords(Math.Abs(number));

            var words = new StringBuilder();

            if (number >= 1000000000)
            {
                long billions = number / 1000000000;
                words.Append(GroupToWords(billions)).Append(" مليار ");
                number %= 1000000000;
                if (number > 0) words.Append("و ");
            }

            if (number >= 1000000)
            {
                long millions = number / 1000000;
                if (millions == 1) words.Append("مليون ");
                else if (millions == 2) words.Append("مليونان ");
                else if (millions >= 3 && millions <= 10) words.Append(GroupToWords(millions)).Append(" ملايين ");
                else words.Append(GroupToWords(millions)).Append(" مليون ");

                number %= 1000000;
                if (number > 0) words.Append("و ");
            }

            if (number >= 1000)
            {
                long thousands = number / 1000;
                if (thousands == 1) words.Append("ألف ");
                else if (thousands == 2) words.Append("ألفان ");
                else if (thousands >= 3 && thousands <= 10) words.Append(GroupToWords(thousands)).Append(" آلاف ");
                else words.Append(GroupToWords(thousands)).Append(" ألف ");

                number %= 1000;
                if (number > 0) words.Append("و ");
            }

            if (number > 0)
            {
                words.Append(GroupToWords(number));
            }

            return words.ToString().Trim();
        }

        private static string GroupToWords(long n)
        {
            var res = new StringBuilder();

            int h = (int)(n / 100);
            int rem = (int)(n % 100);

            if (h > 0)
            {
                res.Append(Hundreds[h]);
                if (rem > 0) res.Append(" و ");
            }

            if (rem > 0)
            {
                if (rem < 20)
                {
                    res.Append(Ones[rem]);
                }
                else
                {
                    int unit = rem % 10;
                    int ten = rem / 10;

                    if (unit > 0)
                    {
                        res.Append(Ones[unit]).Append(" و ");
                    }
                    res.Append(Tens[ten]);
                }
            }

            return res.ToString();
        }
    }
}
