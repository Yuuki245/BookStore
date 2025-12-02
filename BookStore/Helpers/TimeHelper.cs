namespace BookStore.Helpers;

public static class TimeHelper
{
    // Timezone VN: GMT+7
    private static readonly TimeZoneInfo VietnamTimeZone = GetVietnamTimeZone();

    private static TimeZoneInfo GetVietnamTimeZone()
    {
        // Thử các timezone ID phổ biến cho VN
        string[] timeZoneIds = {
            "SE Asia Standard Time",      // Windows
            "Asia/Ho_Chi_Minh",           // Linux/Unix
            "Asia/Saigon"                 // Alternative
        };

        foreach (var id in timeZoneIds)
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(id);
                if (tz != null)
                    return tz;
            }
            catch
            {
                // Continue to next
            }
        }

        // Fallback: Tạo custom timezone GMT+7
        return TimeZoneInfo.CreateCustomTimeZone(
            "Vietnam",
            TimeSpan.FromHours(7),
            "Vietnam Time",
            "Vietnam Time");
    }

    /// <summary>
    /// Lấy thời gian hiện tại theo múi giờ Việt Nam (GMT+7)
    /// </summary>
    public static DateTime GetVietnamTime()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);
    }

    /// <summary>
    /// Convert UTC time sang Vietnam time
    /// </summary>
    public static DateTime ToVietnamTime(DateTime dateTime)
    {
        if (dateTime.Kind == DateTimeKind.Utc)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(dateTime, VietnamTimeZone);
        }
        if (dateTime.Kind == DateTimeKind.Local)
        {
            // Nếu đã là local, giả sử là VN time và return as-is
            // Hoặc convert từ local sang VN (nếu server timezone khác)
            try
            {
                var utc = TimeZoneInfo.ConvertTimeToUtc(dateTime);
                return TimeZoneInfo.ConvertTimeFromUtc(utc, VietnamTimeZone);
            }
            catch
            {
                // Nếu không convert được, giả sử đã là VN time
                return dateTime;
            }
        }
        // Unspecified - giả sử là UTC và convert
        var asUtc = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(asUtc, VietnamTimeZone);
    }

    /// <summary>
    /// Convert Vietnam time sang UTC
    /// </summary>
    public static DateTime ToUtcTime(DateTime vietnamTime)
    {
        if (vietnamTime.Kind == DateTimeKind.Utc)
        {
            return vietnamTime;
        }
        // Giả sử input là VN time (Local hoặc Unspecified)
        var unspecified = DateTime.SpecifyKind(vietnamTime, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, VietnamTimeZone);
    }
}

