using System.Security.Cryptography;

namespace Payments.QRTransfer.Helpers
{
    public class FastQRCodeGenerator
    {
        public static string GenerateFastQRCode(
            string recipientName,   // Alıcının Adı Soyadı
            string recipientIBAN,   // Alıcının IBAN'ı
            string bankCode,
            decimal amount,         // Ödeme Tutarı (TL)
            DateTime createdDate,    // Karekod Oluşturma Zamanı
            DateTime expiryDate,
            bool isDynamic = false,
            string referenceNo = ""
        )
        {
            // Karekod Biçim Göstergesi (Kişiden Kişiye Statik Ödeme)
            string formatIndicator = "750210";

            // Karekod Türü (Statik Karekod)
            string qrType = "";
            if (isDynamic)
                qrType = "010212";
            else
                qrType = "010211";

            // Karekod Üretici Kodu (Örnek kod: 0010)
            string producerCode = $"0204{bankCode}";


            string referenceNumber = "";
            if (string.IsNullOrWhiteSpace(referenceNo))
                referenceNumber = GenerateReferenceNumber();
            else
                referenceNumber = referenceNo;

            // Rastgele 10 haneli Karekod Referans Numarası oluştur
            string referenceField = $"0312{referenceNumber}";


            // Oluşturulma Zamanı (YYAAGGSSDDss formatında)
            string createdTime = createdDate.ToString("yyMMddHHmmss");
            string createdTimeField = $"0612{createdTime}";

            // Varsayılan Son Geçerlilik Zamanı (1 gün sonrası)
            string expiryTime = expiryDate.ToString("yyMMddHHmmss");
            string expiryTimeField = $"0712{expiryTime}";

            string amountField = string.Empty;
            string amountString = string.Empty;
            if (amount != 0m)
            {
                // Tutar (150,50 TL -> 000000015050 formatına çevrilir)
                amountString = amount.ToString("F2").Replace(",", "").Replace(".", "").PadLeft(12, '0');
                amountField = $"5412{amountString}";
            }


            // Karekod Akış Türü (Kişiden Kişiye Ödeme = 03)
            string qrFlowType = "100203";

            // **61 Alanı - Uygulama Şablonu**
            string applicationTemplate = GenerateApplicationTemplate(recipientIBAN, recipientName, qrFlowType);
            string applicationTemplateField = $"61{applicationTemplate.Length:D2}{applicationTemplate}";

            // Hash Değeri (Güvenlik için SHA-256 kullanılır)
            string hashValue = GenerateHash(referenceNumber + recipientIBAN + amountString);
            string hashField = $"2032{hashValue}";

            // Konum (Varsayılan: Ankara - 39.939423, 32.851791)
            string locationField = "50340000000000000000000000000000000000";

            // CRC (Hata kontrol kodu hesaplanır)
            string qrDataWithoutCRC = formatIndicator + qrType + producerCode + referenceField + createdTimeField + expiryTimeField +
                                      amountField + applicationTemplateField + hashField + locationField + "6304";
            string crcValue = ComputeCrcCcitt(qrDataWithoutCRC);

            // Tüm veriyi birleştir
            string finalQRCodeData = qrDataWithoutCRC + crcValue;
            return finalQRCodeData;
        }

        // **61 Alanı için Uygulama Şablonu**
        private static string GenerateApplicationTemplate(string iban, string name, string qrFlowType)
        {
            string ibanField = $"0126{iban}";  // IBAN
            string nameField = $"07{name.Length:D2}{name}";  // Alıcı Adı

            return ibanField + nameField + qrFlowType;
        }

        // Rastgele 10 haneli referans numarası oluştur
        private static string GenerateReferenceNumber()
        {
            Random random = new Random();
            return random.NextInt64(100000000000L, 999999999999L).ToString();
        }

        // SHA-256 Hash Hesaplama
        private static string GenerateHash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("X2")); // Hex formatında yaz
                }
                return sb.ToString().Substring(0, 32); // İlk 32 karakteri al
            }
        }

        private static string ComputeCrcCcitt(string input)
        {
            byte[] data = Encoding.UTF8.GetBytes(input);
            ushort crc = 0xFFFF;  // Başlangıç değeri
            ushort poly = 0x1021; // Polinom değeri

            foreach (byte b in data)
            {
                crc ^= (ushort)(b << 8);
                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x8000) != 0)
                        crc = (ushort)((crc << 1) ^ poly);
                    else
                        crc <<= 1;
                }
            }
            return crc.ToString("X4");
        }
    }

}
