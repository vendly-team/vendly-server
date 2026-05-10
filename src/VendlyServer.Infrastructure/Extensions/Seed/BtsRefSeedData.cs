using VendlyServer.Domain.Entities.Ref;

namespace VendlyServer.Infrastructure.Extensions.Seed;

internal static class BtsRefSeedData
{
    public static IReadOnlyList<BtsRegionRef> Regions(DateTime now) => new[]
    {
        new BtsRegionRef { Code = "01", Name = "Toshkent shahri",            SyncedAt = now },
        new BtsRegionRef { Code = "10", Name = "Toshkent viloyati",          SyncedAt = now },
        new BtsRegionRef { Code = "20", Name = "Sirdaryo viloyati",          SyncedAt = now },
        new BtsRegionRef { Code = "25", Name = "Jizzax viloyati",            SyncedAt = now },
        new BtsRegionRef { Code = "30", Name = "Samarqand viloyati",         SyncedAt = now },
        new BtsRegionRef { Code = "40", Name = "Farg'ona viloyati",          SyncedAt = now },
        new BtsRegionRef { Code = "50", Name = "Namangan viloyati",          SyncedAt = now },
        new BtsRegionRef { Code = "60", Name = "Andijon viloyati",           SyncedAt = now },
        new BtsRegionRef { Code = "70", Name = "Qashqadaryo viloyati",       SyncedAt = now },
        new BtsRegionRef { Code = "75", Name = "Surxondaryo viloyati",       SyncedAt = now },
        new BtsRegionRef { Code = "80", Name = "Buxoro viloyati",            SyncedAt = now },
        new BtsRegionRef { Code = "85", Name = "Navoiy viloyati",            SyncedAt = now },
        new BtsRegionRef { Code = "90", Name = "Xorazm viloyati",            SyncedAt = now },
        new BtsRegionRef { Code = "95", Name = "Qoraqalpog'iston Respublikasi", SyncedAt = now },
    };

    public static IReadOnlyList<BtsCityRef> Cities(DateTime now) => new[]
    {
        // 01 Toshkent shahri (tumanlar)
        New("01", "0101", "Uchtepa tumani", now),
        New("01", "0102", "Yunusobod tumani", now),
        New("01", "0103", "Mirobod tumani", now),
        New("01", "0104", "Chilonzor tumani", now),
        New("01", "0105", "Yashnobod tumani", now),
        New("01", "0106", "Olmazor tumani", now),
        New("01", "0107", "Sergeli tumani", now),
        New("01", "0108", "Mirzo Ulug'bek tumani", now),
        New("01", "0109", "Shayxontohur tumani", now),
        New("01", "0110", "Yakkasaroy tumani", now),
        New("01", "0111", "Bektemir tumani", now),

        // 10 Toshkent viloyati
        New("10", "1001", "Olmaliq", now),
        New("10", "1002", "Angren", now),
        New("10", "1003", "Bekobod", now),
        New("10", "1004", "Chirchiq", now),
        New("10", "1005", "Yangiyo'l", now),
        New("10", "1006", "Nurafshon", now),

        // 20 Sirdaryo viloyati
        New("20", "2001", "Guliston", now),
        New("20", "2002", "Yangiyer", now),
        New("20", "2003", "Shirin", now),
        New("20", "2004", "Boyovut", now),

        // 25 Jizzax viloyati
        New("25", "2501", "Jizzax", now),
        New("25", "2502", "G'allaorol", now),
        New("25", "2503", "Zomin", now),
        New("25", "2504", "Paxtakor", now),

        // 30 Samarqand viloyati
        New("30", "3001", "Samarqand", now),
        New("30", "3002", "Kattaqo'rg'on", now),
        New("30", "3003", "Bulung'ur", now),
        New("30", "3004", "Urgut", now),
        New("30", "3005", "Ishtixon", now),

        // 40 Farg'ona viloyati
        New("40", "4001", "Farg'ona", now),
        New("40", "4002", "Marg'ilon", now),
        New("40", "4003", "Qo'qon", now),
        New("40", "4004", "Quvasoy", now),
        New("40", "4005", "Rishton", now),

        // 50 Namangan viloyati
        New("50", "5001", "Namangan", now),
        New("50", "5002", "Chust", now),
        New("50", "5003", "Pop", now),
        New("50", "5004", "Uchqo'rg'on", now),

        // 60 Andijon viloyati
        New("60", "6001", "Andijon", now),
        New("60", "6002", "Asaka", now),
        New("60", "6003", "Xonobod", now),
        New("60", "6004", "Shahrixon", now),

        // 70 Qashqadaryo viloyati
        New("70", "7001", "Qarshi", now),
        New("70", "7002", "Shahrisabz", now),
        New("70", "7003", "Kitob", now),
        New("70", "7004", "G'uzor", now),

        // 75 Surxondaryo viloyati
        New("75", "7501", "Termiz", now),
        New("75", "7502", "Denov", now),
        New("75", "7503", "Sho'rchi", now),
        New("75", "7504", "Boysun", now),

        // 80 Buxoro viloyati
        New("80", "8001", "Buxoro", now),
        New("80", "8002", "Kogon", now),
        New("80", "8003", "G'ijduvon", now),
        New("80", "8004", "Vobkent", now),

        // 85 Navoiy viloyati
        New("85", "8501", "Navoiy", now),
        New("85", "8502", "Zarafshon", now),
        New("85", "8503", "Nurota", now),
        New("85", "8504", "Karmana", now),

        // 90 Xorazm viloyati
        New("90", "9001", "Urganch", now),
        New("90", "9002", "Xiva", now),
        New("90", "9003", "Pitnak", now),
        New("90", "9004", "Shovot", now),

        // 95 Qoraqalpog'iston
        New("95", "9501", "Nukus", now),
        New("95", "9502", "Mo'ynoq", now),
        New("95", "9503", "Beruniy", now),
        New("95", "9504", "Xo'jayli", now),
        New("95", "9505", "Chimboy", now),
    };

    private static BtsCityRef New(string regionCode, string code, string name, DateTime now) =>
        new() { RegionCode = regionCode, Code = code, Name = name, SyncedAt = now };
}
