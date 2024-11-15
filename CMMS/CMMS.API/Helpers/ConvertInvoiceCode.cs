namespace CMMS.API.Helpers
{
    public static class ConvertInvoiceCode
    {
        public static string ConvertInvoiceCodeToId(string input)
        {
            // Bước 1: Loại bỏ tiền tố "ĐH"
            if (input.StartsWith("ĐH"))
            {
                input = input.Substring(2);
            }

            // Bước 2: Chuyển đổi chuỗi số thành số nguyên
            if (int.TryParse(input, out int number))
            {
                // Bước 3: Chuyển đổi số nguyên thành mảng byte
                byte[] bytes = BitConverter.GetBytes(number);

                // Bước 4: Tạo Guid từ mảng byte
                // Lưu ý: Guid cần 16 byte, vì vậy bạn cần thêm byte 0 nếu cần
                Array.Resize(ref bytes, 16);
                return new Guid(bytes).ToString();
            }
            else
            {
                throw new FormatException("Input string is not in the correct format.");
            }
        }
    }
}
