using System.Globalization;
using System.Text.RegularExpressions;

namespace Payments.QRTransfer.Helpers
{
    public static class StringHelper
    {
        public static string NormalizeString(this string text)
        {
            if (text == null) return null;

            // Kültürel farkları kaldırarak normalize et
            text = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (char c in text)
            {
                // Unicode kategori kontrolü (Harf dışı işaretleri kaldır)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            string result = sb.ToString().Normalize(NormalizationForm.FormC);

            // Türkçe karakterleri ASCII karşılıklarına çevir
            result = result.Replace("Ç", "C").Replace("ç", "c")
                           .Replace("Ğ", "G").Replace("ğ", "g")
                           .Replace("İ", "I").Replace("ı", "i")
                           .Replace("Ö", "O").Replace("ö", "o")
                           .Replace("Ş", "S").Replace("ş", "s")
                           .Replace("Ü", "U").Replace("ü", "u")
                           .Replace("Â", "A").Replace("â", "a")
                           .Replace("Ê", "E").Replace("ê", "e")
                           .Replace("Î", "I").Replace("î", "i")
                           .Replace("Ô", "O").Replace("ô", "o")
                           .Replace("Û", "U").Replace("û", "u")
                           .Replace("Ñ", "N").Replace("ñ", "n")
                           .Replace("Æ", "AE").Replace("æ", "ae")
                           .Replace("Ø", "O").Replace("ø", "o")
                           .Replace("ẞ", "SS").Replace("ß", "ss");

            // Gereksiz noktalama işaretlerini kaldır
            result = new string(result.Where(c => !char.IsPunctuation(c)).ToArray());

            // Fazladan boşlukları temizle (Regex: birden fazla boşluk → tek boşluk)
            result = Regex.Replace(result, @"\s+", " ").Trim();

            return result.ToUpperInvariant(); // Büyük/küçük harf farkını yok say
        }

        public static bool ContainsTarget(this string source, string target)
        {
            var str1 = source.NormalizeString();
            var str2 = target.NormalizeString();

            return str2.Contains(str1, StringComparison.OrdinalIgnoreCase);
        }
    }
}
