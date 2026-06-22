using VendlyServer.Domain.Entities.Common;
using VendlyServer.Domain.Entities.Orders;

namespace VendlyServer.Infrastructure.Extensions.Seed;

internal static class ReturnReasonSeedData
{
    public static IReadOnlyList<ReturnReason> All() => new[]
    {
        new ReturnReason
        {
            Key       = "FACTORY_DEFECT",
            Name      = new MultiLanguageField { Uz = "Zavod nuqsoni",      Ru = "Заводской дефект",                  En = "Factory defect",          Cyrl = "Завод нуқсони" },
            Description = new MultiLanguageField
            {
                Uz   = "Mahsulotda ishlab chiqaruvchi tomonidan yuzaga kelgan texnik yoki tashqi nuqson aniqlangan.",
                Ru   = "В товаре обнаружен технический или внешний дефект, возникший по вине производителя.",
                En   = "A technical or external defect caused by the manufacturer was found in the product.",
                Cyrl = "Маҳсулотда ишлаб чиқарувчи томонидан юзага келган техник ёки ташқи нуқсон аниқланган.",
            },
            CanResell = false,
        },
        new ReturnReason
        {
            Key       = "NOT_WORKING_ON_ARRIVAL",
            Name      = new MultiLanguageField { Uz = "Kelganda ishlamayapti", Ru = "Не работает при получении",       En = "Not working on arrival",  Cyrl = "Келганда ишламаяпти" },
            Description = new MultiLanguageField
            {
                Uz   = "Mahsulot xaridorga yetkazilgan yoki topshirilgan vaqtda umuman ishlamagan.",
                Ru   = "Товар не работал при доставке или передаче покупателю.",
                En   = "The product did not work at all when delivered or handed over to the customer.",
                Cyrl = "Маҳсулот харидорга етказилган ёки топширилган вақтда умуман ишламаган.",
            },
            CanResell = false,
        },
        new ReturnReason
        {
            Key       = "DAMAGED_ON_DELIVERY",
            Name      = new MultiLanguageField { Uz = "Yetkazishda shikastlangan", Ru = "Повреждён при доставке",     En = "Damaged on delivery",     Cyrl = "Етказишда шикастланган" },
            Description = new MultiLanguageField
            {
                Uz   = "Mahsulot yetkazib berish jarayonida sinish, ezilish, tirnalish yoki boshqa jismoniy shikast olgan.",
                Ru   = "Товар получил физические повреждения при доставке: трещины, вмятины, царапины или другие дефекты.",
                En   = "The product was physically damaged during delivery, such as cracks, dents, scratches, or other damage.",
                Cyrl = "Маҳсулот етказиб бериш жараёнида синиш, эзилиш, тирналиш ёки бошқа жисмоний шикаст олган.",
            },
            CanResell = false,
        },
        new ReturnReason
        {
            Key       = "WRONG_PRODUCT_DELIVERED",
            Name      = new MultiLanguageField { Uz = "Noto'g'ri mahsulot yetkazilgan", Ru = "Доставлен неправильный товар", En = "Wrong product delivered", Cyrl = "Нотўғри маҳсулот етказилган" },
            Description = new MultiLanguageField
            {
                Uz   = "Xaridorga buyurtma qilingan mahsulot o'rniga boshqa model, boshqa brend yoki boshqa mahsulot yetkazilgan.",
                Ru   = "Покупателю доставили другую модель, другой бренд или другой товар вместо заказанного.",
                En   = "The customer received a different model, brand, or product instead of the ordered item.",
                Cyrl = "Харидорга буюртма қилинган маҳсулот ўрнига бошқа модель, бошқа бренд ёки бошқа маҳсулот етказилган.",
            },
            CanResell = true,
        },
        new ReturnReason
        {
            Key       = "INCOMPLETE_PACKAGE",
            Name      = new MultiLanguageField { Uz = "Komplekt to'liq emas", Ru = "Неполная комплектация",            En = "Incomplete package",      Cyrl = "Комплект тўлиқ эмас" },
            Description = new MultiLanguageField
            {
                Uz   = "Mahsulot bilan birga kelishi kerak bo'lgan muhim qismlar, aksessuarlar, kabel, pult, qo'llanma yoki kafolat hujjatlari yetishmaydi.",
                Ru   = "Отсутствуют важные детали, аксессуары, кабель, пульт, инструкция или гарантийные документы, которые должны идти в комплекте.",
                En   = "Important parts, accessories, cable, remote, manual, or warranty documents that should be included are missing.",
                Cyrl = "Маҳсулот билан бирга келиши керак бўлган муҳим қисмлар, аксессуарлар, кабель, пульт, қўлланма ёки кафолат ҳужжатлари етишмайди.",
            },
            CanResell = false,
        },
        new ReturnReason
        {
            Key       = "MISSING_WARRANTY_DOCUMENTS",
            Name      = new MultiLanguageField { Uz = "Kafolat hujjatlari yo'q", Ru = "Нет гарантийных документов",   En = "Missing warranty documents", Cyrl = "Кафолат ҳужжатлари йўқ" },
            Description = new MultiLanguageField
            {
                Uz   = "Mahsulot uchun zarur bo'lgan kafolat taloni, chek yoki rasmiy hujjatlar yetishmaydi.",
                Ru   = "Отсутствует гарантийный талон, чек или официальные документы, необходимые для товара.",
                En   = "The warranty card, receipt, or official documents required for the product are missing.",
                Cyrl = "Маҳсулот учун зарур бўлган кафолат талони, чек ёки расмий ҳужжатлар етишмайди.",
            },
            CanResell = false,
        },
        new ReturnReason
        {
            Key       = "SERIAL_NUMBER_MISMATCH",
            Name      = new MultiLanguageField { Uz = "Seriya raqami mos emas", Ru = "Серийный номер не совпадает",   En = "Serial number mismatch",  Cyrl = "Серия рақами мос эмас" },
            Description = new MultiLanguageField
            {
                Uz   = "Mahsulotdagi seriya raqami hujjatlar, chek yoki tizimdagi ma'lumotlar bilan mos kelmaydi.",
                Ru   = "Серийный номер на товаре не совпадает с документами, чеком или данными в системе.",
                En   = "The serial number on the product does not match the documents, receipt, or system records.",
                Cyrl = "Маҳсулотдаги серия рақами ҳужжатлар, чек ёки тизимдаги маълумотлар билан мос келмайди.",
            },
            CanResell = false,
        },
        new ReturnReason
        {
            Key       = "WARRANTY_SERVICE_CONFIRMED_DEFECT",
            Name      = new MultiLanguageField { Uz = "Servis nuqsonni tasdiqladi", Ru = "Сервис подтвердил дефект",  En = "Warranty service confirmed defect", Cyrl = "Сервис нуқсонни тасдиқлади" },
            Description = new MultiLanguageField
            {
                Uz   = "Rasmiy servis markazi mahsulotda kafolat doirasidagi nuqson mavjudligini tasdiqlagan.",
                Ru   = "Официальный сервисный центр подтвердил наличие дефекта по гарантии.",
                En   = "The official service center confirmed a defect covered under warranty.",
                Cyrl = "Расмий сервис маркази маҳсулотда кафолат доирасидаги нуқсон мавжудлигини тасдиқлаган.",
            },
            CanResell = false,
        },
        new ReturnReason
        {
            Key       = "REPEATED_REPAIR_FAILURE",
            Name      = new MultiLanguageField { Uz = "Takroriy ta'mirdan keyin ham nosoz", Ru = "Неисправен после повторного ремонта", En = "Still faulty after repeated repair", Cyrl = "Такрорий таъмирдан кейин ҳам носоз" },
            Description = new MultiLanguageField
            {
                Uz   = "Mahsulot kafolat asosida ta'mirlanganiga qaramay, bir xil yoki jiddiy nosozlik qayta yuzaga kelgan.",
                Ru   = "Несмотря на гарантийный ремонт, та же или серьёзная неисправность возникла повторно.",
                En   = "Despite warranty repair, the same or a serious fault occurred again.",
                Cyrl = "Маҳсулот кафолат асосида таъмирланганига қарамай, бир хил ёки жиддий носозлик қайта юзага келган.",
            },
            CanResell = false,
        },
    };
}
