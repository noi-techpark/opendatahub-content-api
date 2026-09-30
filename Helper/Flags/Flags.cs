// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Helper
{
    public static class EnumHelper
    {
        public static IEnumerable<object> GetValues<T>()
        {
            foreach (object? value in System.Enum.GetValues(typeof(T)))
            {
                if (value != null)
                    yield return value;
            }
        }
    }

    //Packages Weekday
    [Flags]
    public enum WeekdayFlag
    {
        Monday = 1,
        Tuesday = 2,
        Wednesday = 4,
        Thuresday = 8,
        Friday = 16,
        Saturday = 32,
        Sunday = 64,
    }

    #region Accommodations & Packages

    //Boarding for Accommodations and Packages
    [Flags]
    public enum AccoBoardFlag
    {
        [Description("all boards")]
        AllBoards = 0,

        [Description("without board")]
        WithoutBoard = 1,

        [Description("breakfast")]
        Breakfast = 2,

        [Description("half board")]
        HalfBoard = 4,

        [Description("full board")]
        FullBoard = 8,

        [Description("All Inclusive")]
        AllInclusive = 16,
    }

    //Boarding for Accommodations und Packages
    [Flags]
    public enum PackageBoardFlag
    {
        [Description("0")]
        AllBoards = 0,

        [Description("1")]
        without = 2,

        [Description("2")]
        breakfast = 4,

        [Description("3")]
        halfboard = 8,

        [Description("4")]
        fullboard = 16,

        [Description("5")]
        allinclusive = 32,
    }

    //Boarding for Accommodations and Packages
    [Flags]
    public enum HgvBoardFlag
    {
        [Description("price_ws")]
        price_ws = 1,

        [Description("price_bb")]
        price_bb = 2,

        [Description("price_hb")]
        price_hb = 4,

        [Description("price_fb")]
        price_fb = 8,

        [Description("price_ai")]
        price_ai = 16,
    }

    //Accommodationtype for Accommodations
    [Flags]
    public enum AccommodationTypeFlag
    {
        [Description("HotelPension")]
        HotelPension = 1,

        [Description("BedBreakfast")]
        BedBreakfast = 2,

        [Description("Farm")]
        Farm = 4,

        [Description("Camping")]
        Camping = 8,

        [Description("Youth")]
        Youth = 16,

        [Description("Mountain")]
        Mountain = 32,

        [Description("Apartment")]
        Apartment = 64,

        [Description("Notdefined")]
        Notdefined = 128,
    }

    //Category for Accommodations
    [Flags]
    public enum AccommodationCategoryFlag
    {
        [Description("Not categorized")]
        Without = 1,

        [Description("1star")]
        OneStar = 2,

        [Description("1flower")]
        OneFlower = 4,

        [Description("1sun")]
        OneSun = 8,

        [Description("2stars")]
        TwoStars = 16,

        [Description("2flowers")]
        TwoFlowers = 32,

        [Description("2suns")]
        TwoSuns = 64,

        [Description("3stars")]
        ThreeStars = 128,

        [Description("3flowers")]
        ThreeFlowers = 256,

        [Description("3suns")]
        ThreeSuns = 512,

        [Description("3sstars")]
        ThreeSStars = 1024,

        [Description("4stars")]
        FourStars = 2048,

        [Description("4flowers")]
        FourFlowers = 4096,

        [Description("4suns")]
        FourSuns = 8192,

        [Description("4sstars")]
        FourSStars = 16384,

        [Description("5stars")]
        FiveStars = 32768,

        [Description("5flowers")]
        FiveFlowers = 65536,

        [Description("5suns")]
        FiveSuns = 131072,
    }

    //Theme for Accommodations
    [Flags]
    public enum AccoThemeFlag
    {
        [Description("Gourmet")]
        Gourmet = 1,

        [Description("In der Höhe")]
        InderHöhe = 1 << 1,

        [Description("Regionale Wellness")]
        RegionaleWellness = 1 << 2,

        [Description("Biken")]
        Biken = 1 << 3,

        [Description("Familie")]
        Familie = 1 << 4,

        [Description("Wandern")]
        Wandern = 1 << 5,

        [Description("Wein")]
        Wein = 1 << 6,

        [Description("Städtisches Flair")]
        StädtischesFlair = 1 << 7,

        [Description("Am Skigebiet")]
        AmSkigebiet = 1 << 8,

        [Description("Mediterran")]
        Mediterran = 1 << 9,

        [Description("Dolomiten")]
        Dolomiten = 1 << 10,

        [Description("Alpin")]
        Alpin = 1 << 11,

        [Description("Kleine Betriebe")]
        KleineBetriebe = 1 << 12,

        [Description("Hütten und Berggasthöfe")]
        HüttenBerggasthöfe = 1 << 13,

        [Description("Bäuerliche Welten")]
        BäuerlicheWelten = 1 << 14,

        [Description("Bonus Vacanze")]
        BonusVacanze = 1 << 15,

        [Description("Christkindlmarkt")]
        Christkindlmarkt = 1 << 16,

        [Description("Sustainability")]
        Sustainability = 1 << 17,
    }

    //Badges for Accommodations
    [Flags]
    public enum AccoBadgeFlag
    {
        [Description("Wellnesshotel")]
        Wellnesshotel = 1,

        [Description("Familienhotel")]
        Familienhotel = 1 << 1,

        [Description("Bikehotel")]
        Bikehotel = 1 << 2,

        [Description("Bauernhof")]
        Bauernhof = 1 << 3,

        [Description("Behindertengerecht")]
        Behindertengerecht = 1 << 4,

        [Description("Wanderhotel")]
        Wanderhotel = 1 << 5,

        [Description("Südtirol Privat")]
        SuedtirolPrivat = 1 << 6,

        [Description("Vinumhotel")]
        Vinumhotel = 1 << 7,

        [Description("SustainabilityLevel1")]
        SustainabilityLevel1 = 1 << 8,

        [Description("SustainabilityLevel2")]
        SustainabilityLevel2 = 1 << 9,

        [Description("SustainabilityLevel3")]
        SustainabilityLevel3 = 1 << 10,
    }

    //Themes for Packages
    [Flags]
    public enum PackageThemeFlag
    {
        [Description("Wandern")]
        Wandern = 1, //1,

        [Description("Rad & Mountainbike")]
        RadMountainbike = 1 << 1, // 2,

        [Description("Familie")]
        Familie = 1 << 2, //4,

        [Description("Wellness Gesundheit")]
        WellnessGesundheit = 1 << 3, //8,

        [Description("Essen und Trinken")]
        EssenTrinken = 1 << 4, //16,

        [Description("Golf")]
        Golf = 1 << 5, //32,

        [Description("Kultur")]
        Kultur = 1 << 6, //64,

        [Description("Motorsport")]
        Motorsport = 1 << 7, //128,

        [Description("Ohne Auto im Urlaub")]
        OhneAutoimUrlaub = 1 << 8, //256,

        [Description("Ski & Snowboard")]
        SkiSnowboard = 1 << 9, //512,

        [Description("Sommer Aktiv")]
        SommerAktiv = 1 << 10, //1024,

        [Description("Veranstaltungen")]
        Veranstaltungen = 1 << 11, //2048,

        [Description("Weihnachtsmärkte")]
        Weihnachtsmaerkte = 1 << 12, //4096,

        [Description("Winter Aktiv")]
        WinterAktiv = 1 << 13, //8192,

        [Description("Vitalpina")]
        Vitalpina = 1 << 14, //16384,

        [Description("Vitalpina: Durchatmen")]
        VitalpinaDurchatmen = 1 << 15, //32768,

        [Description("Bikehotels: EBike")]
        BikehotelsEBike = 1 << 16, //65536,

        [Description("Bikehotels Freeride")]
        BikehotelsFreeride = 1 << 17, //131072,

        [Description("Bikehotels Mountainbike")]
        BikehotelsMountainbike = 1 << 18, //524288,

        [Description("Bikehotels Radwandern")]
        BikehotelsRadwandern = 1 << 19, //1048576,

        [Description("Bikehotels: Rennrad")]
        BikehotelsRennrad = 1 << 20, //2097152,

        [Description("Familienhotels")]
        Familienhotels = 1 << 21, //4194304,

        [Description("Familienhotels: Naturdetektiv")]
        FamilienhotelsNaturdetektiv = 1 << 22, //8388608,

        [Description("Familienhotel")]
        Familienhotel = 1 << 23, //33554432,

        [Description("Naturdetektiv Sommer")]
        NaturdetektivSommer = 1 << 24, //67108864,

        [Description("Naturdetektiv Winter")]
        NaturdetektivWinter = 1 << 25, //134217728

        [Description("Südtirol Balance")]
        SuedtirolBalance = 1 << 26, //134217728
    }

    //Features for Accommodations
    [Flags]
    public enum AccoFeatureFlag
    {
        [Description("Gruppenfreundlich")]
        Gruppenfreundlich = 1,

        [Description("Tagung")]
        Tagung = 1 << 1,

        [Description("Schwimmbad")]
        Schwimmbad = 1 << 2,

        [Description("Sauna")]
        Sauna = 1 << 3,

        [Description("Garage")]
        Garage = 1 << 4,

        [Description("Abholservice")]
        Abholservice = 1 << 5,

        [Description("Wlan")]
        Wlan = 1 << 6,

        [Description("Barrierefrei")]
        Barrierefrei = 1 << 7,

        [Description("Allergikerküche")]
        Allergikerküche = 1 << 8,

        [Description("Kleine Haustiere")]
        KleineHaustiere = 1 << 9,

        [Description("Guestcard")]
        Guestcard = 1 << 10,
    }

    #endregion

    #region Gastronomies

    //Kategorie für Gastronomy
    [Flags]
    public enum GastroCategoryFlag
    {
        [Description("B0BDC4C2C5938D9B734D97B09C8A47A4")]
        Restaurant = 1,

        [Description("9095FC003A3E2F393D63A54682359B37")]
        BarCafeBistro = 1 << 1,

        [Description("59FE0B38EB7F4AC3951A5F477A0E1FA2")]
        PubDisco = 1 << 2,

        [Description("43D095A3FE8A450099D33926BBC1ADF8")]
        ApresSki = 1 << 3,

        [Description("8176B5A707E2067708AF18045E068E15")]
        Jausenstation = 1 << 4,

        [Description("AC56B3717C3152A428A1D338A638C570")]
        Pizzeria = 1 << 5,

        [Description("E8883A596A2463A9B3E1586C9E780F17")]
        BauerlicherSchankbetrieb = 1 << 6,

        [Description("700B02F1BE96B01C34CCF7A637DB3054")]
        Buschenschank = 1 << 7,

        [Description("4A14E16888CB07C18C65A6B59C5A19A7")]
        Hofschank = 1 << 8,

        [Description("AB320B063588EA95F45505E940903115")]
        ToerggeleLokale = 1 << 9,

        [Description("33B86F5B91A08A0EFD6854DEB0207205")]
        Schnellimbiss = 1 << 10,

        [Description("29BC7A9AE7CF173FBCCE6A48DD001229")]
        Mensa = 1 << 11,

        [Description("C3CC9C83C32BFA4E9A05133291EA9FFB")]
        VinothekWeinhausTaverne = 1 << 12,

        [Description("6A2A32E2BFEE270083351B0CFD9BA2E3")]
        Eisdiele = 1 << 13,

        [Description("9B158D17F03509C46037C3C7B23F2FE4")]
        Gasthaus = 1 << 14,

        [Description("D8B8ABEDD17A139DEDA2695545C420D6")]
        Gasthof = 1 << 15,

        [Description("902D9BA559B1ED889694284F05CFA41E")]
        Braugarten = 1 << 16,

        [Description("2328C37167BBBC5776831B8A262A6C36")]
        Schutzhuette = 1 << 17,

        [Description("8025DB5CFCBA4FF281DDDE1F2B1D19A2")]
        Alm = 1 << 18,

        [Description("B916489A77C94D8D92B03184EE587A31")]
        Skihuette = 1 << 19,

        [Description("6A1EA69EC83F4E9CBDED5317D18B4BFB")]
        Weingut = 1 << 20,
    }

    //Dishcodes für Gastronomy
    [Flags]
    public enum GastroDishcodeFlag
    {
        [Description("A130EB1985EC41CFB199528BE038399B")]
        Speisen = 1,

        [Description("B539399E53D348049B9E710A2B22E74D")]
        Vorspeise = 1 << 1,

        [Description("A7601BBA081B4D48A50634E029B3D75A")]
        Hauptspeise = 1 << 2,

        [Description("E7B9475EC5B24B6F830FBD0339D48F9D")]
        Nachspeise = 1 << 3,

        [Description("EB7532946781423D9932121F3D1D7CC4")]
        Tagesgericht = 1 << 4,

        [Description("78A13E3381FA4A71B21118BDDF84BAFB")]
        Menue = 1 << 5,

        [Description("6284265E90D24C909E23A176EEB3B6F7")]
        Degustationsmenue = 1 << 6,

        [Description("AD8426538FCF4D8A81E06BE044088BAA")]
        Kindermenues = 1 << 7,

        [Description("5C84265DA5F84F84A7896808ACCB675A")]
        Mittagsmenues = 1 << 8,
    }

    //Ceremonycodes für Gastronomy
    [Flags]
    public enum GastroCeremonyFlag
    {
        [Description("DEC7019ADE6B46CDAE87584821D9B4DB")]
        Familienfeiern = 1,

        [Description("648773AE1BBD4001B85DC88E7592ACE2")]
        Hochzeiten = 1 << 1,

        [Description("085CF94B4F25440AA079E88D8DBA45C2")]
        Geburtstagsfeiern = 1 << 2,

        [Description("0A7DD92FA86B47D18DBFCB5572A93C9F")]
        Firmenessen = 1 << 3,

        [Description("22C75C83F99F4D6FADF6D82F7754B4C1")]
        Weihnachtsessen = 1 << 4,

        [Description("4FBC28A456AA43E6B01D8BD1072D8CE6")]
        Silvestermenü = 1 << 5,

        [Description("94E42C7211B9430B8F096ABB7ED59AC2")]
        SeminareTagungen = 1 << 6,

        [Description("38DCFB491D27408C990654CB64C6339D")]
        Versammlungen = 1 << 7,

        [Description("41543CA256A8433C8CD47109FFCEE1F7")]
        SitzplatzAussenbereich = 1 << 8,
    }

    //Facilities für Gastronomy
    [Flags]
    public enum GastroFacilityFlag : long
    {
        [Description("BBD9085F89BC417B97D986A26CE86F40")]
        Kreditkarte = 1L,

        [Description("9F05BA64D6614894A89FFE23A4A0F20B")]
        BankomatMaestro = 1L << 1,

        [Description("B7E9EE4A91544849B69D5A5564DDCDFB")]
        Barrierefrei = 1L << 2,

        [Description("93BDE34283FF41899CCF530BE80201E2")]
        Garten = 1L << 3,

        [Description("3A89DBB5F633473096C902D2CBFD2FA3")]
        Raucherraum = 1L << 4,

        [Description("36C354DC30F14DD7B1CCFEE78E82132C")]
        Spielzimmer = 1L << 5,

        [Description("188A9BADC0324C10B0013F108CE5EA5C")]
        Spielplatz = 1L << 6,

        [Description("481891DECDAF443E92E9957B5EC8FCAC")]
        Parkplaetze = 1L << 7,

        [Description("D579D1C8EA8445018CA5BB6DABEA0C26")]
        Garage = 1L << 8,

        [Description("D9DCDD52FE444818AAFAB0E02FD92D91")]
        Hundeerlaubt = 1L << 9,

        [Description("452422597831423F9F4E2B1A2BA9177A")]
        Biergarten = 1L << 10,

        [Description("63534DC188314AC68DAB0EF0DE6EE5B0")]
        Terrasse = 1L << 11,

        [Description("B3BC8F4D7BA948369515FBA8075D47DB")]
        Wintergarten = 1L << 12,

        [Description("52281FC851CA11D18F1400A02427D15E")]
        GeeignetfuerBusse = 1L << 13,

        [Description("5228206E51CA11D18F1400A02427D15E")]
        Bierbar = 1L << 14,

        [Description("46AD7938616B4D4882A006BEF3B199A4")]
        GaultMillauSuedtirol = 1L << 15,

        [Description("F0A385D0E8E44944AFCA3893712A1420")]
        Guidaespresso = 1L << 16,

        [Description("2FA54F6F350748AE9CD1A389A5C9EDD9")]
        Gamberorosso = 1L << 17,

        [Description("C0E761D71CC44F4C80D75FF68ED72C55")]
        Feinschmecker = 1L << 18,

        [Description("6797D594C7BF4C7AA6D384B234EC7C44")]
        AralSchlemmerAtlas = 1L << 19,

        [Description("E5775068F5644E92B7CF94BDFCDA5175")]
        VartaFuehrer = 1L << 20,

        [Description("1FFD5352501542BF8BCB24B7BF75CF4F")]
        Bertelsmann = 1L << 21,

        [Description("1641B07E28B9443EAB53E1DB7363F6F3")]
        PreisfuerSuedtirolerWeinkultur = 1L << 22,

        [Description("5060F78090604B2E97A96D86B97D2E0B")]
        Michelin = 1L << 23,

        [Description("ED4028BEE0164BF185B923B3DD4FF9A0")]
        RoterHahn = 1L << 24,

        [Description("0DBA881DD41340FDA76196EBCEFC9ECD")]
        Tafelspitz = 1L << 25,

        [Description("6C72999B96594EC08281DE9CCA00EF75")]
        SuedtirolerGasthaus = 1L << 26,

        //NEU
        [Description("098EB30324EA492DBD99F323AE20A621")]
        KostenlosesWlan = 1L << 27,

        [Description("79EDF6ABA6F8484583D38DFDE9758B80")]
        KleintierStreichelzoo = 1L << 28,

        [Description("DB1DDC1CC1EF4449BECD341C9DC12611")]
        SlowFood = 1L << 29,

        [Description("B92FFCECB4574BB9BCAE5EAB8917602D")]
        ilgattiMassobrio = 1L << 30,

        [Description("80BA0C6E19524ECA8956BA65294C8E5A")]
        guidaOsterieItalia = 1L << 31,

        [Description("19A210AEE7C84D539D92A61FE30B2375")]
        Takeawayvorbestellung = 1L << 32,

        [Description("B48D00D84F164E12822262183F6B8298")]
        Takeaway = 1L << 33,

        [Description("45411D13F45946B3925E74B4DE78AF7B")]
        Deliveryservicevorbestellung = 1L << 34,

        [Description("B5DA5FEE25F648A5930FFD36AAF1EE10")]
        Deliveryservice = 1L << 35,

        [Description("CFE311C2420142EFB46B71041D9DBA08")]
        Onlinetischreservierung = 1L << 36,

        [Description("3BECD8556B474044B8417C942D58C3DB")]
        Nachhaltigkeitslabelsuedtirol1 = 1L << 37,

        [Description("BA990D98B89944F0BFB9D318DA6281EC")]
        Nachhaltigkeitslabelsuedtirol2 = 1L << 38,

        [Description("B8E1E0E654BE48C988266D94F26CC014")]
        Nachhaltigkeitslabelsuedtirol3 = 1L << 39,

        [Description("2A58CAA843B34E7C8696C387B215132C")]
        Testedstaff = 1L << 40,
    }

    //Dishcodes für Gastronomy
    [Flags]
    public enum GastroCuisineFlag
    {
        [Description("3091F5B92F534F67986C08151E6F4454")]
        VegetarischesMenu = 1,

        [Description("71A7D4A821F7437EA1DC05CEE9655A5A")]
        GlutenfreieKueche = 1 << 1,

        [Description("11A6BEA7EEFC4716BDF8FBD5E15C0CFB")]
        LaktosefreieKost = 1 << 2,

        [Description("A469B187953944A0AF49C5EBE13DCF00")]
        VeganeKueche = 1 << 3,

        [Description("F42DBD202D6E4289AF48D138DA09ECB7")]
        WarmeKueche = 1 << 4,

        [Description("2476B5BBAEB7467C9A0099F06D0ED004")]
        SuedtirolerSpezialitaeten = 1 << 5,

        [Description("30DC854F943D42CF8DB140CF4A90EC7E")]
        GourmetKueche = 1 << 6,

        [Description("D1F124A123554B14AB9600F2313ED051")]
        ItalienischeKueche = 1 << 7,

        [Description("CB8AF7CB80E844758B18E9C4E2D84035")]
        InternationaleKueche = 1 << 8,

        [Description("50FFF83EB75944DE9F6F15CC51E85E7A")]
        Pizza = 1 << 9,

        [Description("6322DE8AFE8E406F886E7C40D0DC1ADD")]
        Fischspezialitaeten = 1 << 10,

        [Description("0E9721E540FB4D84BADC0DFA24F0543B")]
        AsiatischeKueche = 1 << 11,

        [Description("C48E7E7679B04835B6744650E129BABF")]
        Wildspezialitaeten = 1 << 12,

        [Description("167850CF26984D50A59A5F42EB24A0AD")]
        ProdukteeigenerErzeugung = 1 << 13,

        [Description("4F9335FDAB834B11B36CD4C163F990A7")]
        Diaetkueche = 1 << 14,

        [Description("69621AE51DF942A1BBED32D460E65132")]
        Grillspezialitaeten = 1 << 15,

        [Description("22F0D9C42B06423EB63E1F2F27B7CA3A")]
        LadinischeKueche = 1 << 16,

        [Description("BC08B00995564BB28997C55C870120D1")]
        KleineKarte = 1 << 17,

        [Description("0E55D7C2A7BC4866BF8438C522C17254")]
        Fischwochen = 1 << 18,

        [Description("FC627623C6994E37927F6048E32B79C2")]
        Spargelwochen = 1 << 19,

        [Description("21A903DE35654070803DFDDF29C67291")]
        Lammwochen = 1 << 20,

        [Description("8E28215F82BA430EA016BA5D1C776A30")]
        Wildwochen = 1 << 21,

        [Description("D413EF912D18462CA0055A44F55351D1")]
        Vorspeisenwochen = 1 << 22,

        [Description("BC6B57D90AFB496098DD0D059D04EE7C")]
        Nudelwochen = 1 << 23,

        [Description("B36D855D60CB4D79BA78F3FEFEE9F9D3")]
        Kräuterwochen = 1 << 24,

        [Description("AD8426538FCF4D8A81E06BE044088BAA")]
        Kindermenues = 1 << 25,

        [Description("5C84265DA5F84F84A7896808ACCB675A")]
        Mittagsmenues = 1 << 26,

        [Description("C84CAC645211448E8C8A8A92A9B1A830")]
        Brunch = 1 << 27,

        [Description("0AB2257C7F984065B5132D1DD68D205A")]
        Glutenfreiepizza = 1 << 28,
    }

    #endregion

    #region ODHActivityPois

    [Flags]
    public enum SmgPoiMainTypeFlag
    {
        [Description("Activity")]
        Activity = 1,

        [Description("Poi")]
        Poi = 1 << 1,

        [Description("Gastronomy")]
        Gastronomy = 1 << 2,
    }

    [Flags]
    public enum SmgPoiTypeFlag
    {
        [Description("Wellness Entspannung")]
        WellnessEntspannung = 1,

        [Description("Winter")]
        Winter = 1 << 1,

        [Description("Sommer")]
        Sommer = 1 << 2,

        [Description("Kultur Sehenswürdigkeiten")]
        KulturSehenswuerdigkeiten = 1 << 3,

        [Description("Anderes")]
        Anderes = 1 << 4,

        [Description("Essen Trinken")]
        EssenTrinken = 1 << 5,

        [Description("Mobilität")]
        Mobility = 1 << 6,

        [Description("Geschäfte und Dienstleister")]
        ShopsAndServices = 1 << 7,
    }

    //Subtype

    [Flags]
    public enum SmgPoiSubTypeFlagWellness
    {
        [Description("Wellnessbehandlungen")]
        Wellnessbehandlungen = 1,

        [Description("Therme Wasserwelten")]
        ThermeWasserwelten = 1 << 1,
    }

    [Flags]
    public enum SmgPoiSubTypeFlagWinter
    {
        [Description("Weihnachtsmärkte")]
        Weihnachtsmaerkte = 1,

        [Description("Eisklettern")]
        Eisklettern = 1 << 1,

        [Description("Eislaufen")]
        Eislaufen = 1 << 2,

        [Description("Langlaufen")]
        Langlaufen = 1 << 3,

        [Description("Pferdeschlittenfahrten")]
        Pferdeschlittenfahrten = 1 << 4,

        [Description("Rodeln")]
        Rodeln = 1 << 5,

        [Description("Skirundtouren Pisten")]
        SkirundtourenPisten = 1 << 6,

        [Description("Snowparks")]
        Snowparks = 1 << 7,

        [Description("Skischulen Skiverleih")]
        SkischulenSkiverleih = 1 << 8,

        [Description("Skitouren")]
        Skitouren = 1 << 9,

        [Description("Schneeschuhwandern")]
        Schneeschuhwandern = 1 << 10,

        [Description("Winterwandern")]
        Winterwandern = 1 << 11,

        [Description("Skigebiete")]
        Skigebiete = 1 << 12,
    }

    [Flags]
    public enum SmgPoiSubTypeFlagSommer
    {
        [Description("Wandern")]
        Wandern = 1,

        [Description("Bergsteigen")]
        Bergsteigen = 1 << 1,

        [Description("Klettern")]
        Klettern = 1 << 2,

        [Description("Radfahren Radtouren")]
        RadfahrenRadtouren = 1 << 3,

        [Description("Radverleih")]
        Radverleih = 1 << 4,

        [Description("Badeseen Freibäder")]
        BadeseenFreibaeder = 1 << 5,

        [Description("Wassersport")]
        Wassersport = 1 << 6,

        [Description("Reiten")]
        Reiten = 1 << 7,

        [Description("Freizeit Erlebnis")]
        FreizeitErlebnis = 1 << 8,

        [Description("Laufen Fitness")]
        LaufenFitness = 1 << 9,

        [Description("Golf")]
        Golf = 1 << 10,

        [Description("Paragleiten")]
        Paragleiten = 1 << 11,

        [Description("Angeln Fischen")]
        AngelnFischen = 1 << 12,
    }

    [Flags]
    public enum SmgPoiSubTypeFlagKultur
    {
        [Description("Architektur")]
        Architektur = 1,

        [Description("Kirchen Klöster")]
        KirchenKloester = 1 << 1,

        [Description("Museen")]
        Museen = 1 << 2,

        [Description("Naturparkhäuser")]
        Naturparkhaeuser = 1 << 3,

        [Description("Burgen Schlösser")]
        BurgenSchloesser = 1 << 4,

        [Description("Bergwerke")]
        Bergwerke = 1 << 5,

        [Description("Sehenswerte Plätze")]
        SehenswertePlaetze = 1 << 6,
    }

    [Flags]
    public enum SmgPoiSubTypeFlagAnderes
    {
        [Description("Ohne Zuordnung")]
        OhneZuordnung = 1,

        [Description("Familienurlaub")]
        Familienurlaub = 1 << 1,

        [Description("Sagen")]
        Sagen = 1 << 2,

        [Description("Klettertour")]
        Klettertour = 1 << 3,

        [Description("Aufstiegsanlagen")]
        Aufstiegsanlagen = 1 << 4,

        [Description("Stadtrundgang")]
        Fitnessparcours = 1 << 5,

        //NEW
        [Description("Guides Schulen")]
        GuidesSchulen = 1 << 6,

        [Description("Spiel und Sportanlagen")]
        SpielSportAnlagen = 1 << 7,

        [Description("Verleihe")]
        Verleihe = 1 << 8,

        [Description("Parks und Grünanlagen")]
        ParksGruenanlagen = 1 << 9,

        [Description("Kulturzentren und Theater")]
        KulturzentrenundTheater = 1 << 10,

        [Description("Weitere Kultur und Sehenswürdigkeiten")]
        WeitereKultur = 1 << 11,

        [Description("Vereine")]
        Vereine = 1 << 12,

        [Description("Unternehmen")]
        Unternehmen = 1 << 13,
    }

    [Flags]
    public enum SmgPoiSubTypeFlagEssenTrinken
    {
        [Description("Andere Gastronomiebetriebe")]
        AndereGastronomiebetriebe = 1,

        [Description("Weinkellereien")]
        Weinkellereien = 1 << 1,

        [Description("Restaurants Gasthäuser")]
        RestaurantsGasthauser = 1 << 2,

        [Description("Hütten Almen")]
        HuettenAlmen = 1 << 3,

        [Description("Bäuerliche Schankbetriebe")]
        BauerlicheSchankbetriebe = 1 << 4,
    }

    [Flags]
    public enum SmgPoiSubTypeFlagMobility
    {
        [Description("Bushaltestellen")]
        Bushaltestellen = 1,

        [Description("Parkplätze")]
        Parkplaetze = 1 << 1,

        [Description("Tankstellen Benzin Diesel")]
        TankstellenBenzinDiesel = 1 << 2,

        [Description("E-Auto Ladestation")]
        ETankstellenLadestationen = 1 << 3,

        [Description("Taxi, Mietwagen mit Fahrer, Bus")]
        TaxiMietwagenmitFahrerBus = 1 << 4,

        [Description("Zugbahnhöfe")]
        Zugbahnhoefe = 1 << 5,

        [Description("Carsharing")]
        Carsharing = 1 << 6,

        [Description("Tankstellen Methan")]
        TankstellenMethan = 1 << 7,

        [Description("Tankstellen Autogas")]
        TankstellenAutogas = 1 << 8,

        [Description("Tankstellen Wasserstoff")]
        TankstellenWasserstoff = 1 << 9,

        [Description("Weitere Verkehr und Transport")]
        WeitereVerkehrundTransport = 1 << 10,

        [Description("E-Bike Ladestation")]
        EBikeLadestation = 1 << 11,

        [Description("Fahrradbox")]
        Fahrradbox = 1 << 12,

        [Description("Verkaufstellen Ticket Oeffentliche Verkehrsmittel")]
        VerkaufstellenTicketOeffentlicheVerkehrsmittel = 1 << 13,

        [Description("Mautstelle")]
        Mautstelle = 1 << 14,

        [Description("Autobahnraststaette")]
        Autobahnraststaette = 1 << 15,
    }

    [Flags]
    public enum SmgPoiSubTypeFlagShopsAndServices : long
    {
        [Description("Gesundheit")]
        Gesundheit = 1,

        [Description("Geschäfte")]
        Geschaefte = 1 << 1,

        [Description("Dienstleister")]
        Dienstleister = 1 << 2,

        [Description("Öffentliche Einrichtungen")]
        OeffentlicheEinrichtung = 1 << 3,

        [Description("Nachtleben und Unterhaltung")]
        NachtlebenUnterhaltung = 1 << 4,
    }

    //POI Type Health
    [Flags]
    public enum SmgPoiPoiTypeFlagGesundheit
    {
        ////Ärzte Apotheken
        [Description("Apotheken")]
        Apotheken = 1,

        [Description("Ärzte")]
        Aerzte = 1 << 1,

        [Description("Feriendialysen")]
        Feriendialysen = 1 << 2,

        [Description("Kinderärzte")]
        Kinderaerzte = 1 << 3,

        [Description("Tierärzte")]
        Tieraerzte = 1 << 4,

        [Description("Zahnärzte")]
        Zahnaerzte = 1 << 5,

        [Description("Medizinausgabestellen, Medikamentenautomaten")]
        MedizinausgabestellenMedikamentenautomatn = 1 << 6,

        [Description("Privatkliniken, Trauma Zentren, Physiotherapeuten")]
        PrivatklinikenTraumaZentrenPhysiotherapeuten = 1 << 7,

        [Description("Weitere Ärzte, Apotheken")]
        WeitereAertzeApotheken = 1 << 8,
    }

    //POI Type Shops
    [Flags]
    public enum SmgPoiPoiTypeFlagGeschaefte : long
    {
        //Geschäfte
        [Description("Getränke")]
        Getraenke = 1L,

        [Description("Antiquitäten")]
        Antiquitaeten = 1L << 1,

        [Description("Blumen")]
        Blumen = 1L << 2,

        [Description("Computerzubehör, Technik")]
        ComputerzubehoerTechnik = 1L << 3,

        [Description("Drogerie")]
        Drogerie = 1L << 4,

        [Description("Fahrräder")]
        Fahrraeder = 1L << 5,

        [Description("Farben, Tapeten")]
        FarbenTapeten = 1L << 6,

        [Description("Fleisch und Wurstwaren")]
        FleischundWurstwaren = 1L << 7,

        [Description("Brot und Gebäck")]
        BrotundGebaeck = 1L << 8,

        [Description("Haushaltswaren")]
        Haushaltswaren = 1L << 9,

        [Description("Juweliere, Goldschmiede")]
        JuweliereGoldschmiede = 1L << 10,

        [Description("Kunsthandwerke")]
        Kunsthandwerke = 1L << 11,

        [Description("Landwirtschaftliche Artikel")]
        LandwirtschaftlicheArtikel = 1L << 12,

        [Description("Lebensmittel")]
        Lebensmittel = 1L << 13,

        [Description("Lederwaren, Schuhe")]
        LederwarenSchuhe = 1L << 14,

        [Description("Lokale traditionelle Produkte")]
        LokaletraditionelleProdukte = 1L << 15,

        [Description("Mode, Bekleidung")]
        ModeBekleidung = 1L << 16,

        [Description("Obst- und Gemüse")]
        ObstundGemuese = 1L << 17,

        [Description("Optiker, Foto")]
        OptikerFoto = 1L << 18,

        [Description("Produktionsstätten, Hofläden")]
        ProduktionsstaettenHoflaeden = 1L << 19,

        [Description("Souvenir")]
        Souvenir = 1L << 20,

        [Description("Spielwaren")]
        Spielwaren = 1L << 21,

        [Description("Sportartikel")]
        Sportartikel = 1 << 22,

        [Description("Tierbedarf")]
        Tierbedarf = 1L << 23,

        [Description("Zeitungen, Bücher und Papierwaren")]
        ZeitungenBuecherundPapierwaren = 1L << 24,

        [Description("Kindermode")]
        Kindermode = 1L << 25,

        [Description("Auto und Motor")]
        AutoundMotor = 1L << 26,

        [Description("Bausektor, Handwerk")]
        BausektorHandwerk = 1L << 27,

        [Description("Reinigungen")]
        Reinigungen = 1L << 28,

        [Description("Weitere Geschäfte")]
        WeitereGeschaefte = 1L << 29,

        [Description("Wohnausstattung")]
        Wohnausstattung = 1L << 30,
    }

    //POI Type Dienstleister
    [Flags]
    public enum SmgPoiPoiTypeFlagDienstleister : long
    {
        //Diensleister
        [Description("Auto und Motor")]
        AutoundMotor = 1L,

        [Description("Bausektor, Handwerk")]
        BausektorHandwerk = 1L << 1,

        [Description("Kinderbetreuung Animation")]
        KinderbetreuungAnimation = 1L << 2,

        [Description("Werbung und Grafik")]
        WerbungundGrafik = 1L << 3,

        [Description("Reinigung")]
        Reinigung = 1L << 4,

        [Description("Tierpflege")]
        Tierpflege = 1L << 5,

        [Description("Bank, Bankomat")]
        BankBankomat = 1L << 6,

        [Description("Beauty und Wellness")]
        BeautyundWellness = 1L << 7,

        [Description("Friseur")]
        Friseur = 1L << 8,

        [Description("Kosmetik")]
        Kosmetik = 1L << 9,

        [Description("Massage, Heilbäder")]
        MassageHeilbaeder = 1L << 10,

        [Description("Day Spa, Sauna")]
        DaySpaSauna = 1L << 11,

        [Description("Kneippbäder")]
        Kneippbaeder = 1L << 12,

        [Description("Weitere Beauty und Wellness")]
        WeitereBeautyundWellness = 1L << 13,

        [Description("Weitere Dienstleister")]
        WeitereDienstleister = 1L << 14,

        //Handwerk
        [Description("Bildhauer")]
        Bildhauer = 1L << 15,

        [Description("Kunstwebereien")]
        Kunstwebereien = 1L << 16,

        [Description("Federkielstickereien")]
        Federkielstickereien = 1L << 17,

        [Description("Kunstmaler und Vergolder")]
        KunstmalerundVergolder = 1L << 18,

        [Description("Weitere Kunsthandwerker")]
        WeitereHandwerker = 1L << 19,

        [Description("Künstler")]
        Kuenstler = 1L << 20,
    }

    //POI Type Public Services
    [Flags]
    public enum SmgPoiPoiTypeFlagOeffentlichEinrichtungen
    {
        //Öffentliche Einrichtungen
        [Description("Bibliotheken")]
        Bibliotheken = 1,

        [Description("Gemeinden")]
        Gemeinden = 1 << 1,

        [Description("Öffentliches WLANs")]
        OeffentlichesWLANs = 1 << 2,

        [Description("Polizei, Carabinieri")]
        PolizeiCarabinieri = 1 << 3,

        [Description("Post")]
        Post = 1 << 4,

        [Description("Recyclinghöfe")]
        Recyclinghoefe = 1 << 5,

        [Description("Infobüro")]
        Infobuero = 1 << 6,

        [Description("Infopoint")]
        Infopoint = 1 << 7,

        [Description("WCs")]
        WCs = 1 << 8,

        [Description("Krankenhaus")]
        Krankenhaus = 1 << 9,

        [Description("Weitere öffentliche Einrichtungen")]
        WeitereoeffentlicheEinrichtungen = 1 << 10,

        [Description("Trinkwasserauffüllpunkte")]
        Trinkwasserauffuellpunkte = 1 << 11,
    }

    //POI Type Nachtleben Unterhaltung
    [Flags]
    public enum SmgPoiPoiTypeFlagNachtlebenUnterhaltung
    {
        //Nachtleben Unterhaltung
        [Description("Biergärten")]
        Biergaerten = 1,

        [Description("Cocktailbars")]
        Cocktailbars = 1 << 1,

        [Description("Diskotheken, Nachtclubs")]
        DiskothekenNachtclubs = 1 << 2,

        [Description("Weitere Nachtleben und Unterhaltungen")]
        WeitereNachtlebenundUnterhaltungen = 1 << 3,
    }

    //Poi Poi Type

    [Flags]
    public enum SmgPoiPoiTypeFlagWandern
    {
        [Description("Fernwanderwege")]
        Fernwanderwege = 1,

        [Description("Höhenwege")]
        Hoehenwege = 1 << 1,

        [Description("Themenwanderungen")]
        Themenwanderungen = 1 << 2,

        [Description("Waalwege")]
        Waalwege = 1 << 3,
    }

    [Flags]
    public enum SmgPoiPoiTypeFlagKlettern
    {
        [Description("Klettersteige")]
        Klettersteige = 1,

        [Description("Kletterparks")]
        Kletterparks = 1 << 1,

        [Description("Hochseilgärten")]
        Hochseilgaerten = 1 << 2,

        [Description("Kletterhallen")]
        Kletterhallen = 1 << 3,
    }

    [Flags]
    public enum SmgPoiPoiTypeFlagRadfahren
    {
        [Description("Radtouren")]
        Radtouren = 1,

        [Description("Talradwege")]
        Talradwege = 1 << 1,

        [Description("Mountainbike")]
        Mountainbike = 1 << 2,

        [Description("Rennrad")]
        Rennrad = 1 << 3,

        [Description("Freeride")]
        Freeride = 1 << 4,

        [Description("Downhill")]
        Downhill = 1 << 5,
    }

    [Flags]
    public enum SmgPoiPoiTypeFlagMuseen
    {
        [Description("Museen Kultur")]
        MuseenKultur = 1,

        [Description("Museen Natur")]
        MuseenNatur = 1 << 1,

        [Description("Museen Technik")]
        MuseenTechnik = 1 << 2,

        [Description("Museen Kunst")]
        MuseenKunst = 1 << 3,

        [Description("Museen Montags geöffnet")]
        MuseenMontagsgeoffnet = 1 << 4,

        [Description("Museen Eintritt frei")]
        MuseenEintrittfrei = 1 << 5,

        [Description("Museen Ausstellungen")]
        MuseenAusstellungen = 1 << 6,
    }

    [Flags]
    public enum SmgPoiPoiTypeFlagFreizeitErlebnis
    {
        [Description("Beachvolleyball")]
        Beachvolleyball = 1,

        [Description("Freizeitparks")]
        Freizeitparks = 1 << 1,

        [Description("Minigolf")]
        Minigolf = 1 << 2,
    }

    [Flags]
    public enum SmgPoiPoiTypeFlagKirchenKloester
    {
        [Description("Kirchen")]
        Kirchen = 1,

        [Description("Klöster")]
        Kloester = 1 << 1,
    }

    [Flags]
    public enum SmgPoiPoiTypeFlagLaufenFitness
    {
        [Description("Laufstrecken")]
        Laufstrecken = 1,

        [Description("Nordic Walking")]
        NordicWalking = 1 << 1,
    }

    [Flags]
    public enum SmgPoiPoiTypeFlagThermeWasserwelten
    {
        [Description("Hallenbad")]
        Hallenbad = 1,

        [Description("Therme")]
        Therme = 1 << 1,
    }

    [Flags]
    public enum SmgPoiPoiTypeFlagWeihnachtsmaerkte
    {
        [Description("Ländliche Christkindlmärkte")]
        LaendlicheChristkindlmaerkte = 1,

        [Description("Original Südtiroler Christkindlmärkte")]
        OriginalSuedtirolerChristkindlmaerkte = 1 << 1,
    }

    [Flags]
    public enum SmgPoiPoiTypeFlagLanglaufen
    {
        [Description("Klassisch")]
        Klassisch = 1,

        [Description("Freistil")]
        Freistil = 1 << 1,
    }

    [Flags]
    public enum SmgPoiPoiTypeFlagRodelbahnen
    {
        [Description("Rodelbahn")]
        Rodelbahn = 1,

        [Description("Alpin Bob")]
        AlpinBob = 1 << 1,
    }

    [Flags]
    public enum SmgPoiPoiTypeFlagSkischulen
    {
        [Description("Skischulen")]
        Skischulen = 1,

        [Description("Skiverleih")]
        Skiverleih = 1 << 1,
    }

    [Flags]
    public enum SmgPoiPoiTypeFlagBurgenSchloesser
    {
        [Description("Ruine")]
        Skischulen = 1,
    }

    [Flags]
    public enum SmgPoiPoiTypeFlagBadeseenFreibaeder
    {
        [Description("Schwimmbäder")]
        Schwimmbaeder = 1,

        [Description("Badeseen")]
        Badeseen = 1 << 1,
    }

    [Flags]
    public enum SmgPoiPoiTypeFlagArchitektur
    {
        [Description("Architektur Kulinarik")]
        Kulinarik = 1,

        [Description("Architektur Kunst Kultur")]
        KunstKultur = 1 << 1,

        [Description("Architektur Freizeit")]
        Freizeit = 1 << 2,

        [Description("Architektur Öffentliche Gebäude")]
        OeffentlicheGebaeude = 1 << 3,

        [Description("Architektur Wohnbauten")]
        Wohnbauten = 1 << 4,

        [Description("Architektur Wohnen Hotels")]
        WohnenHotels = 1 << 5,
    }

    [Flags]
    public enum SmgPoiPoiTypeFlagAndereGastronomiebetriebe
    {
        [Description("Pub Disco")]
        PubDisco = 1,

        [Description("Apres Ski")]
        ApresSki = 1 << 1,

        [Description("Jausenstation")]
        Jausenstation = 1 << 2,

        [Description("Törggele Lokal")]
        ToerggeleLokal = 1 << 3,

        [Description("Schnellimbiss")]
        Schnellimbiss = 1 << 4,

        [Description("Mensa")]
        Mensa = 1 << 5,

        [Description("Eisdiele")]
        Eisdiele = 1 << 6,

        [Description("Braugarten")]
        Braugarten = 1 << 7,
    }

    [Flags]
    public enum SmgPoiPoiTypeFlagRestaurantsGasthauser
    {
        [Description("Restaurants")]
        Restaurants = 1,

        [Description("Gasthäuser Gasthöfe")]
        GasthauserGasthoefe = 1 << 1,

        [Description("Pizzerias")]
        Pizzerias = 1 << 2,

        [Description("Vinotheken")]
        Vinotheken = 1 << 3,

        [Description("Bars Cafes Bistros")]
        BarsCafesBistros = 1 << 4,

        [Description("Gasthaus")]
        Gasthaus = 1 << 5,

        [Description("Gasthof")]
        Gasthof = 1 << 6,
    }

    [Flags]
    public enum SmgPoiPoiTypeFlagHuettenAlmen
    {
        [Description("Schutzhütten")]
        Schutzhuetten = 1,

        [Description("Almen")]
        Almen = 1 << 1,

        [Description("Skihütten")]
        Skihuetten = 1 << 2,
    }

    [Flags]
    public enum SmgPoiPoiTypeFlagBauerlicheSchankbetriebe
    {
        [Description("Buschen Hofschänke")]
        BuschenHofschaenke = 1,

        [Description("Buschenschank")]
        Buschenschank = 1 << 1,

        [Description("Hofschank")]
        Hofschank = 1 << 2,
    }

    [Flags]
    public enum SmgPoiPoiTypeFlagSkigebiete
    {
        [Description("Dolomiti Superski")]
        DolomitiSuperski = 1,

        [Description("Ortler Skiarena")]
        OrtlerSkiarena = 1 << 1,

        [Description("Skiregion Tauferer Ahrntal")]
        TaufererAhrntal = 1 << 2,

        [Description("Skiverbund Eisacktal-Wipptal")]
        SkiverbundEisacktalerWipptal = 1 << 3,
    }

    [Flags]
    public enum SmgPoiPoiTypeFlagGuidesSchulen
    {
        [Description("Ski- und Bergführer")]
        SkiBergfuehrer = 1,

        [Description("Bikeguides")]
        Bikeguides = 1 << 1,

        [Description("Wanderführer")]
        Wanderfuehrer = 1 << 2,

        [Description("Weitere Guides Schulen")]
        Weitere = 1 << 3,
    }

    [Flags]
    public enum SmgPoiPoiTypeFlagSpielundSportanlagen
    {
        [Description("Bogenschiessanlagen")]
        Bogenschiessanlagen = 1,

        [Description("Fitnesscenter")]
        Fitnesscenter = 1 << 1,

        [Description("Fussballplätze")]
        Fußballplaetze = 1 << 2,

        [Description("Kinderspielplätze")]
        Kinderspielplaetze = 1 << 3,

        [Description("Pferdekutschenfahrten")]
        Pferdekutschenfahrten = 1 << 4,

        [Description("Radraststätten")]
        Radraststaetten = 1 << 5,

        [Description("Tennisplätze")]
        Tennisplaetze = 1 << 6,

        [Description("Zip-lines")]
        Ziplines = 1 << 7,

        [Description("Weitere Spiel und Sportanlagen")]
        WeitereSpielSportanlagen = 1 << 8,

        [Description("Tierpark")]
        Tierpark = 1 << 9,

        [Description("Bikepark")]
        Bikepark = 1 << 10,

        [Description("Pumptracks und mehr")]
        Pumptracksundmehr = 1 << 11,
    }

    [Flags]
    public enum SmgPoiPoiTypeFlagAufstiegsanlagen
    {
        [Description("nicht definiert")]
        nichtdefiniert = 1,

        [Description("Seilbahn")]
        Seilbahn = 1 << 1,

        [Description("Umlaufbahn")]
        Umlaufbahn = 1 << 2,

        [Description("Kabinenbahn")]
        Kabinenbahn = 1 << 3,

        [Description("Unterirdische Bahn")]
        UnterirdischeBahn = 1 << 4,

        [Description("Sessellift")]
        Sessellift = 1 << 5,

        [Description("Skilift")]
        Skilift = 1 << 6,

        [Description("Schrägaufzug")]
        Schraegaufzug = 1 << 7,

        [Description("Standseilbahn Zahnradbahn")]
        StandseilbahnZahnradbahn = 1 << 8,

        [Description("Telemix")]
        Telemix = 1 << 9,

        [Description("Förderband")]
        Foerderband = 1 << 10,

        [Description("1er Sessellift kuppelbar")]
        EinserSesselLiftkuppelbar = 1 << 11,

        [Description("2er Sessellift kuppelbar")]
        ZweierSesselliftkuppelbar = 1 << 12,

        [Description("3er Sessellift kuppelbar")]
        DreierSesselliftkuppelbar = 1 << 13,

        [Description("4er Sessellift kuppelbar")]
        ViererSesselliftkuppelbar = 1 << 14,

        [Description("6er Sessellift kuppelbar")]
        SechserSesselliftkuppelbar = 1 << 15,

        [Description("8er Sessellift kuppelbar")]
        AchterSesselliftkuppelbar = 1 << 16,

        [Description("Klein-Skilift")]
        KleinSkilift = 1 << 17,

        //NEU
        [Description("Skibus")]
        Skibus = 1 << 18,

        [Description("Zug")]
        Zug = 1 << 19,

        [Description("Weitere Aufstiegsanlagen")]
        WeitereAufstiegsanlagen = 1 << 20,
    }

    [Flags]
    public enum SmgPoiPoiTypeFlagVereine
    {
        [Description("Sport")]
        Sport = 1,

        [Description("Kultur, Musik, Theater")]
        KulturMusikTheater = 1 << 1,

        [Description("Tier, Natur, Wissenschaft")]
        TierNaturWissenschaft = 1 << 2,

        [Description("Gesellschaft, Politisch")]
        GesellschaftPolitisch = 1 << 3,

        [Description("Sozial, Rettung, gemeinnützig")]
        SozialRettungGemeinnuetzig = 1 << 4,

        [Description("Freizeitverein")]
        Freizeitverein = 1 << 5,

        [Description("weitere Vereine")]
        WeitereVereine = 1 << 6,
    }

    [Flags]
    public enum SmgPoiPoiTypeFlagUnternehmen
    {
        [Description("Industrie")]
        Industrie = 1,

        [Description("Produktion")]
        Produktion = 1 << 1,

        [Description("Baugewerbe")]
        Baugewerbe = 1 << 2,

        [Description("Handwerk")]
        Handwerk = 1 << 3,

        [Description("Energie")]
        Energie = 1 << 4,

        [Description("weitere Unternehmen")]
        WeitereUnternehmen = 1 << 5,
    }

    #endregion

    #region Articles

    [Flags]
    public enum ArticleTypeFlag
    {
        [Description("basisartikel")]
        basisartikel = 1, //1

        [Description("buchtippartikel")]
        buchtippartikel = 1 << 1, //2

        [Description("contentartikel")]
        contentartikel = 1 << 2, //4

        [Description("veranstaltungsartikel")]
        veranstaltungsartikel = 1 << 3, //8

        [Description("presseartikel")]
        presseartikel = 1 << 4, //16

        [Description("rezeptartikel")]
        rezeptartikel = 1 << 5, //32

        [Description("reiseveranstalter")]
        reiseveranstalter = 1 << 6, //64

        [Description("b2bartikel")]
        b2bartikel = 1 << 7, //128

        [Description("idmartikel")]
        idmartikel = 1 << 8, //256

        [Description("specialannouncement")]
        specialannouncement = 1 << 9, //512

        [Description("newsfeednoi")]
        newsfeednoi =
            1
            << 10 //1024
        ,
    }

    [Flags]
    public enum ArticleBasisArticleFlag
    {
        [Description("Suggestion")]
        Suggestion = 1,

        [Description("Hotel")]
        Hotel = 1 << 1,

        [Description("Gastronomy")]
        Gastronomy = 1 << 2,

        [Description("General")]
        General = 1 << 3,

        [Description("Tip")]
        Tip = 1 << 4,

        [Description("News")]
        News = 1 << 5,

        [Description("Detail")]
        Detail = 1 << 6,
    }

    [Flags]
    public enum ArticleBuchTippFlag
    {
        [Description("Accommodation-Restaurants")]
        AccommodationRestaurants = 1,

        [Description("Biking")]
        Biking = 1 << 1,

        [Description("Culture-History")]
        CultureHistory = 1 << 2,

        [Description("Family")]
        Family = 1 << 3,

        [Description("Gastronomy")]
        Gastronomy = 1 << 4,

        [Description("Health")]
        Health = 1 << 5,

        [Description("Novels-Poetry")]
        NovelsPoetry = 1 << 6,

        [Description("Travel-guides")]
        Travelguides = 1 << 7,

        [Description("Trekking-guides")]
        Trekkingguides = 1 << 8,

        [Description("Winter")]
        Winter = 1 << 9,
    }

    [Flags]
    public enum ArticlePresseArticleFlag
    {
        [Description("allgemeinepresseartikel")]
        allgemeinepresseartikel = 1,

        [Description("pressemeetings")]
        pressemeetings = 1 << 1,

        [Description("pressemitteilungen")]
        pressemitteilungen = 1 << 2,

        [Description("pressethemenserviceartikel")]
        pressethemenserviceartikel = 1 << 3,
    }

    [Flags]
    public enum ArticleB2BArticleFlag
    {
        [Description("B2BDetail")]
        allgemeinepresseartikel = 1,

        [Description("B2BProgrammTipp")]
        pressemeetings = 1 << 1,

        [Description("B2BNeuigkeiten")]
        pressemitteilungen = 1 << 2,

        [Description("B2BVeranstaltung")]
        pressethemenserviceartikel = 1 << 3,
    }

    [Flags]
    public enum ArticleContentArticleFlag
    {
        [Description("Site Content")]
        SiteContent = 1,

        [Description("B2B Site Content")]
        B2BSiteContent = 1 << 1,

        [Description("Press Site Content")]
        PressSiteContent = 1 << 2,
    }

    [Flags]
    public enum ArticleIDMArticleFlag
    {
        [Description("genussbotschafter")]
        genussbotschafter = 1,

        [Description("hersteller")]
        hersteller = 1 << 1,
    }

    [Flags]
    public enum ArticleVeranstaltungsArticleFlag
    {
        [Description("Gastronomie")]
        Gastronomie = 1,

        [Description("Treffen")]
        Treffen = 1 << 1,

        [Description("Volksfest")]
        Volksfest = 1 << 2,

        [Description("Tradition")]
        Tradition = 1 << 3,

        [Description("Kinder/Familie")]
        KinderFamilie = 1 << 4,

        [Description("Kultur")]
        Kultur = 1 << 5,

        [Description("Kunsthandwerk")]
        Kunsthandwerk = 1 << 6,

        [Description("Theater")]
        Theater = 1 << 7,

        [Description("Wanderungen")]
        Wanderungen = 1 << 8,

        [Description("Ausstellungen/Kunst")]
        AusstellungenKunst = 1 << 9,

        [Description("Sport")]
        Sport = 1 << 10,

        [Description("Messen")]
        Messen = 1 << 11,

        [Description("Musik")]
        Musik = 1 << 12,

        [Description("Geführte Touren")]
        GefuehrteTouren = 1 << 13,

        [Description("Unterhaltung")]
        Unterhaltung = 1 << 14,
    }

    #endregion

    #region Events

    //Event Topics
    [Flags]
    public enum EventTopicFlag : long
    {
        [Description("0D25868CC23242D6AC97AEB2973CB3D6")]
        TagungenVortraege = 1L, //1,

        [Description("162C0067811B477DA725D2F5F2D98398")]
        Sport = 1L << 1, // 2,

        [Description("252200A028C8449D9A6205369A6D0D36")]
        GastronomieTypischeProdukte = 1L << 2, //4,

        [Description("33BDC54BD39946F4852B3394B00610AE")]
        HandwerkBrauchtum = 1L << 3, //8,

        [Description("4C4961D9FC5B48EEB73067BEB9D4402A")]
        MessenMaerkte = 1L << 4, //16,

        [Description("6884FE362C88434B9F49725E3328112B")]
        TheaterVorfuehrungen = 1L << 5, //32,

        [Description("767F6F43FC394CE9A3C8A9725C6FF134")]
        KurseBildung = 1L << 6, //64,

        [Description("7E048074BA004EC58E29E330A9AA476B")]
        MusikTanz = 1L << 7, //128,

        [Description("9C3449EE278C4D94AA5A7C286729DEA0")]
        VolksfesteFestivals = 1L << 8, //256,

        [Description("ACE8B613F2074A7BB59C0B1DD40A43CD")]
        WanderungenAusfluege = 1L << 9, //512,

        [Description("B5467FEFE5C74FA5AD32B83793A76165")]
        FuehrungenBesichtigungen = 1L << 10, //1024,

        [Description("C72CE969B98947FABC99CBC7B033F28E")]
        AusstellungenKunst = 1L << 11, //2048,

        [Description("D98B49DF24C342D09A8161836435CF86")]
        Familie = 1L << 12, //4096,

        [Description("08A37CD6550244179510BED319C7B972")]
        PilzeSammeln = 1L << 13, //8192,

        [Description("1281B6BE26C14CE8AC00D99DD77273D5")]
        Verleih = 1L << 14, //16384,

        [Description("18791378F74745049C51D82E82143461")]
        GuestCard = 1L << 15, //32768,

        [Description("1CA441D2561944E5BB6BB0A477D88E79")]
        Langlauf = 1L << 16, //65536,

        [Description("2D54FAB93F3741C7A81107B298D8FA2D")]
        Verkostung = 1L << 17, //131072,

        [Description("8CE148DBE21A4F36B44B99A1498FDB5F")]
        Bustransferreise = 1L << 18, //262144,

        [Description("B12E42FB1AE940539425B1A78A0818B0")]
        Parkplatz = 1L << 19, //524288,

        [Description("BDF62AD595F54F0E8148894A6A1CC3CF")]
        Durchfahrt = 1L << 20, //1048576,

        [Description("CA7B7B8B21C24D75B3B641DE30254AE6")]
        Shop = 1L << 21, //2097152,

        [Description("F6FABAA6C6C3475FBF2157287DAA8A28")]
        Bergbahn = 1L << 22, //4194304,

        [Description("1186AFACB68D4AEDA19487ADF62BA1BC")]
        Restaurant = 1L << 23, //4194304,

        [Description("2E067878076C4FB0B0C17F6D731006B8")]
        Buchvorstellung = 1L << 24, //4194304,

        [Description("B7EB85224DA445A080707D9CA2957860")]
        Startgebuehrparagleiten = 1L << 25, //4194304,
    }

    #endregion

    #region Venues

    [Flags]
    public enum VenueCategoryFlag
    {
        [Description("VEN03D5630AD1E1438DBA1E998F0F72B626")]
        type_nonConventional = 1, //1,

        [Description("VEN410C6C89ED0041559B815FFC112870BB")]
        type_fair = 1 << 1, // 2,

        [Description("VEN5B35118C14FF4CB9A5546DDD6461AB62")]
        type_historicalResidence = 1 << 2, //4,

        [Description("VEN7324F5B3E210450C8DAF4AE3C5354ED5")]
        type_otherConferenceSite = 1 << 3, //8,

        [Description("VENA09AA4B479384DFBBB1605E9E8C0A93B")]
        type_conferenceHotel = 1 << 4, //16,

        [Description("VENAF924020EB32412C8F7FC5B0CB7DD8B3")]
        type_institutionalSite = 1 << 5, //32,

        [Description("VENDD2171604D37485582E9475A63198933")]
        type_conventionCenter = 1 << 6, //64,

        [Description("VEN0219B13D32614E84B30221509F95E0C9")]
        type_other = 1 << 7,
    }

    [Flags]
    public enum VenueSeatTypeFlag
    {
        [Description("VENE4AA0F13DDF1410489F237FF6B181831")]
        seattype_audience = 1, //1,

        [Description("VEN0536C64E86B44FC2A2FBA77B6948993D")]
        seattype_class = 1 << 1, // 2,

        [Description("VEN3D1C707DCA794BBFBAA76ECA49465865")]
        seattype_board = 1 << 2, //4,

        [Description("VENDF70389DB7C54D57BCA0778AB7AA9C73")]
        seattype_horseshoe = 1 << 3, //8,

        [Description("VEN6E29B13DB4B04860B97DC993E203B1CF")]
        seattype_cocktail = 1 << 4, //16,

        [Description("VENDB7A4015D38543C5B8971362BDE1986A")]
        seattype_galaDinner =
            1
            << 5 //32,
        ,
    }

    [Flags]
    public enum VenueFeatureFlag
    {
        [Description("VEN24059D25A6FC4FAE936E7CB3E776A646")]
        faci_wardrobe = 1, //1,

        [Description("VEN2BF0A6C5BED846E7B35F84BB2CC39145")]
        faci_barrierFreeAccess = 1 << 1, // 2,

        [Description("VEN3BB8A8EA44CC465AAA91F97FA60A2666")]
        faci_heating = 1 << 2, //4,

        [Description("VEN479B65D5C62D44CE935E59F677477FF8")]
        faci_table = 1 << 3, //8,

        [Description("VEN600538A7883A4C74B6E8DE68DA2C7C04")]
        faci_food = 1 << 4, //16,

        [Description("VEN646D4B0F026243BFAF56709424BA53E1")]
        faci_projector = 1 << 5, //32,

        [Description("VEN7B56AA4CA95844B4AFA6E02DA4A234B9")]
        faci_darkening = 1 << 6, //64,

        [Description("VEN7CC12B6F18A5468180DA26EFB8314FFD")]
        faci_independentAccess = 1 << 7, //128,

        [Description("VEN8D08DF7A9A1A4FA6AA7AC3DF8FD76194")]
        faci_parking = 1 << 8, //256,

        [Description("VEN94ED27485E8743539BAA2F5B3A6B8AE2")]
        faci_receptionRoom = 1 << 9, //512,

        [Description("VENAF97953298494A5BBF4DE00DCC2724ED")]
        faci_microphone = 1 << 10, //1024,

        [Description("VENB7363C4DCE004B6FB163779B61577284")]
        faci_projectorScreen = 1 << 11, //2048,

        [Description("VENCE38E6D7EED44B8C898F6CCD53276CEB")]
        faci_airConditioning = 1 << 12, //4096,

        [Description("VEND6B34DA7BA5847A7B0154998D80FD5FC")]
        faci_lan = 1 << 13, //8192,

        [Description("VENEC3EE1CABD4C4507B3A6BC6511BDDEC5")]
        faci_bar = 1 << 14, //16384,

        [Description("VENF5D451A120074F8F8D3BE961BBE9B526")]
        faci_wifi = 1 << 15, //32768,
    }

    #endregion

    #region ActivityData

    [Flags]
    public enum ActivityTypeFlag
    {
        [Description("Berg")]
        Berg = 1,

        [Description("Radfahren")]
        Radfahren = 1 << 1,

        [Description("Stadtrundgang")]
        Stadtrundgang = 1 << 2,

        [Description("Pferdesport")]
        Pferdesport = 1 << 3,

        [Description("Wandern")]
        Wandern = 1 << 4,

        [Description("Laufen und Fitness")]
        LaufenundFitness = 1 << 5,

        [Description("Loipen")]
        Loipen = 1 << 6,

        [Description("Rodelbahnen")]
        Rodelbahnen = 1 << 7,

        [Description("Piste")]
        Piste = 1 << 8,

        [Description("Aufstiegsanlagen")]
        Aufstiegsanlagen = 1 << 9,
    }

    [Flags]
    public enum ActivityTypePisten
    {
        [Description("nicht definiert")]
        nichtdefiniert = 1,

        [Description("Ski alpin")]
        Skialpin = 1 << 1, //2

        //[Description("Halfpipe")]
        //Halfpipe = 1 << 2,  //4
        //[Description("Tiefschnee")]
        //Tiefschnee = 1 << 3, //8
        [Description("Snowpark")]
        Snowpark = 1 << 2,

        //[Description("Kids-Funpark")]
        //KidsFunpark = 1 << 5, //
        //[Description("Geschwindigkeitspiste")]
        //Geschwindigkeitspiste = 1 << 6,
        //[Description("Slalompiste")]
        //Slalompiste = 1 << 7,
        [Description("Rundkurs")]
        Rundkurs = 1 << 3,

        [Description("Weitere Pisten")]
        WeiterePisten = 1 << 4,
    }

    [Flags]
    public enum ActivityTypeAufstiegsanlagen
    {
        [Description("nicht definiert")]
        nichtdefiniert = 1,

        [Description("Seilbahn")]
        Seilbahn = 1 << 1,

        [Description("Umlaufbahn")]
        Umlaufbahn = 1 << 2,

        [Description("Kabinenbahn")]
        Kabinenbahn = 1 << 3,

        [Description("Unterirdische Bahn")]
        UnterirdischeBahn = 1 << 4,

        [Description("Sessellift")]
        Sessellift = 1 << 5,

        [Description("Skilift")]
        Skilift = 1 << 6,

        [Description("Schrägaufzug")]
        Schraegaufzug = 1 << 7,

        [Description("Standseilbahn Zahnradbahn")]
        StandseilbahnZahnradbahn = 1 << 8,

        [Description("Telemix")]
        Telemix = 1 << 9,

        [Description("Förderband")]
        Foerderband = 1 << 10,

        [Description("2er Sessellift kuppelbar")]
        ZweierSesselliftkuppelbar = 1 << 11,

        [Description("3er Sessellift kuppelbar")]
        DreierSesselliftkuppelbar = 1 << 12,

        [Description("4er Sessellift kuppelbar")]
        ViererSesselliftkuppelbar = 1 << 13,

        [Description("6er Sessellift kuppelbar")]
        SechserSesselliftkuppelbar = 1 << 14,

        [Description("8er Sessellift kuppelbar")]
        AchterSesselliftkuppelbar = 1 << 15,

        [Description("Klein-Skilift")]
        KleinSkilift = 1 << 16,

        //NEU
        [Description("Skibus")]
        Skibus = 1 << 17,

        [Description("1er Sessellift kuppelbar")]
        EinserSesselLiftkuppelbar = 1 << 18,

        [Description("Zug")]
        Zug = 1 << 19,

        [Description("Weitere Aufstiegsanlagen")]
        WeitereAufstiegsanlagen = 1 << 20,
    }

    [Flags]
    public enum ActivityTypeBerg
    {
        [Description("Alpinklettern")]
        Alpinklettern = 1,

        [Description("Bergtouren")]
        Bergtouren = 1 << 1,

        [Description("Hochtouren")]
        Hochtouren = 1 << 2,

        [Description("Klettersteige")]
        Klettersteige = 1 << 3,

        [Description("Schneeschuhtouren")]
        Schneeschuhtouren = 1 << 4,

        [Description("Skitouren")]
        Skitouren = 1 << 5,

        [Description("Weitere Berge")]
        WeitereBerge = 1 << 6,

        [Description("Eisklettern")]
        Eisklettern = 1 << 7,

        [Description("Eistour")]
        Eistour = 1 << 8,
    }

    [Flags]
    public enum ActivityTypeLaufenFitness
    {
        [Description("Bergläufe")]
        Berglaeufe = 1,

        [Description("Fitnessparcours")]
        Fitnessparcours = 1 << 1,

        [Description("Innline Skating")]
        InnlineSkating = 1 << 2,

        [Description("Laufstrecken")]
        Laufstrecken = 1 << 3,

        [Description("Nordic Walkings")]
        NordicWalkings = 1 << 4,

        [Description("Trail running")]
        Trailrunning = 1 << 5,

        [Description("Weitere Laufen und Fitness")]
        WeitereLaufenundFitness = 1 << 6,
    }

    [Flags]
    public enum ActivityTypeLoipen
    {
        [Description("Klassisch")]
        Klassisch = 1,

        [Description("Skating")]
        Skating = 1 << 1,

        [Description("Klassisch und Skating")]
        KlassischundSkating = 1 << 2,
    }

    [Flags]
    public enum ActivityTypeOrtstouren
    {
        [Description("Ortstouren")]
        Ortstouren = 1,

        [Description("Weitere Ortstouren")]
        WeitereOrtstouren = 1 << 1,
    }

    [Flags]
    public enum ActivityTypePferde
    {
        [Description("Trail für Kutschenfahrten")]
        Kutschen = 1,

        [Description("Trail für Reitpferde")]
        Reitpferde = 1 << 1,

        [Description("Weitere Pferde")]
        WeiterePferde = 1 << 2,
    }

    [Flags]
    public enum ActivityTypeRadfahren
    {
        [Description("Downhills, Freerides")]
        DownhillsFreerides = 1,

        [Description("E-Bikes")]
        EBikes = 1 << 1,

        [Description("Fernradwege")]
        Fernradwege = 1 << 2,

        [Description("Mountainbikes")]
        Mountainbikes = 1 << 3,

        [Description("Mountainbike Transalps")]
        MountainbikeTransalps = 1 << 4,

        [Description("Radtouren")]
        Radtouren = 1 << 5,

        [Description("Rennräder")]
        Rennraeder = 1 << 6,

        [Description("Weitere Radfahren")]
        WeitereRadfahren = 1 << 7,

        [Description("Pumptrack")]
        Pumptrack = 1 << 8,

        [Description("Singletrail Freeride")]
        SingleTrailFreeride = 1 << 9,

        [Description("E-MTB")]
        EMtb = 1 << 10,
    }

    [Flags]
    public enum ActivityTypeRodeln
    {
        [Description("Schienenrodelbahn")]
        Schienenrodelbahn = 1,

        [Description("Eisbahnen")]
        Eisbahnen = 1 << 1,

        [Description("Rodelbahnen")]
        Rodelbahnen = 1 << 2,

        [Description("Schneebahnen")]
        Schneebahnen = 1 << 3,

        [Description("Weitere Rodeln")]
        WeitereRodeln = 1 << 4,
    }

    [Flags]
    public enum ActivityTypeWandern
    {
        [Description("Barrierefrei")]
        Barrierefrei = 1,

        [Description("Familienwanderungen")]
        Familienwanderungen = 1 << 1,

        [Description("Fernwanderwege")]
        Fernwanderwege = 1 << 2,

        [Description("Pilgerwege")]
        Pilgerwege = 1 << 3,

        [Description("Schneeschuhwanderungen")]
        Schneeschuhwanderungen = 1 << 4,

        [Description("Themenwanderungen")]
        Themenwanderungen = 1 << 5,

        [Description("Winterwanderungen")]
        Winterwanderungen = 1 << 6,

        [Description("Kinderwagen tauglich")]
        Kinderwagentauglich = 1 << 7,

        [Description("Waalweg")]
        Waalweg = 1 << 8,

        [Description("Weitere Wandern")]
        WeitereWandern = 1 << 9,
    }

    #endregion

    #region POIData

    [Flags]
    public enum PoiTypeFlag
    {
        [Description("Ärzte, Apotheken")]
        AertzeApotheken = 1,

        [Description("Geschäfte")]
        GeschaefteDienstleister = 1 << 1,

        [Description("Kultur und Sehenswürdigkeiten")]
        KulturSehenswuerdigkeiten = 1 << 2,

        [Description("Nachtleben und Unterhaltung")]
        NachtlebenUnterhaltung = 1 << 3,

        [Description("Öffentliche Einrichtungen")]
        OeffentlicheEinrichtungen = 1 << 4,

        [Description("Sport und Freizeit")]
        SportFreizeit = 1 << 5,

        [Description("Verkehr und Transport")]
        VerkehrTransport = 1 << 6,

        [Description("Dienstleister")]
        Dienstleister = 1 << 7,

        [Description("Kunsthandwerker")]
        Handwerk = 1 << 8,

        [Description("Vereine")]
        Vereine = 1 << 9,

        [Description("Unternehmen")]
        Unternehmen = 1 << 10,
    }

    [Flags]
    public enum PoiTypeAerzteApotheken
    {
        [Description("Apotheken")]
        Apotheken = 1,

        [Description("Ärzte")]
        Aerzte = 1 << 1,

        [Description("Feriendialysen")]
        Feriendialysen = 1 << 2,

        [Description("Kinderärzte")]
        Kinderaerzte = 1 << 3,

        [Description("Tierärzte")]
        Tieraerzte = 1 << 4,

        [Description("Zahnärzte")]
        Zahnaerzte = 1 << 5,

        [Description("Medizinausgabestellen, Medikamentenautomatn")]
        MedizinausgabestellenMedikamentenautomatn = 1 << 6,

        [Description("Privatkliniken, Trauma Zentren, Physiotherapeuten")]
        PrivatklinikenTraumaZentrenPhysiotherapeuten = 1 << 7,

        [Description("Weitere Ärzte, Apotheken")]
        WeitereAertzeApotheken = 1 << 8,
    }

    [Flags]
    public enum PoiTypeGeschaefte : long
    {
        [Description("Getränke")]
        Getraenke = 1L,

        [Description("Antiquitäten")]
        Antiquitaeten = 1L << 1,

        [Description("Blumen")]
        Blumen = 1L << 2,

        [Description("Computerzubehör, Technik")]
        ComputerzubehoerTechnik = 1L << 3,

        [Description("Drogerie")]
        Drogerie = 1L << 4,

        [Description("Fahrräder")]
        Fahrraeder = 1L << 5,

        [Description("Farben, Tapeten")]
        FarbenTapeten = 1L << 6,

        [Description("Fleisch und Wurstwaren")]
        FleischundWurstwaren = 1L << 7,

        [Description("Brot und Gebäck")]
        BrotundGebaeck = 1L << 8,

        [Description("Haushaltswaren")]
        Haushaltswaren = 1L << 9,

        [Description("Juweliere, Goldschmiede")]
        JuweliereGoldschmiede = 1L << 10,

        [Description("Kunsthandwerke")]
        Kunsthandwerke = 1L << 11,

        [Description("Landwirtschaftliche Artikel")]
        LandwirtschaftlicheArtikel = 1L << 12,

        [Description("Lebensmittel")]
        Lebensmittel = 1L << 13,

        [Description("Lederwaren, Schuhe")]
        LederwarenSchuhe = 1L << 14,

        [Description("Lokale traditionelle Produkte")]
        LokaletraditionelleProdukte = 1L << 15,

        [Description("Mode, Bekleidung")]
        ModeBekleidung = 1L << 16,

        [Description("Obst- und Gemüse")]
        ObstundGemuese = 1L << 17,

        [Description("Optiker, Foto")]
        OptikerFoto = 1L << 18,

        [Description("Produktionsstätten, Hofläden")]
        ProduktionsstaettenHoflaeden = 1L << 19,

        [Description("Souvenir")]
        Souvenir = 1L << 20,

        [Description("Spielwaren")]
        Spielwaren = 1L << 21,

        [Description("Sportartikel")]
        Sportartikel = 1 << 22,

        [Description("Tierbedarf")]
        Tierbedarf = 1L << 23,

        [Description("Zeitungen, Bücher und Papierwaren")]
        ZeitungenBuecherundPapierwaren = 1L << 24,

        [Description("Kindermode")]
        Kindermode = 1L << 25,

        [Description("Auto und Motor")]
        AutoundMotor = 1L << 26,

        [Description("Bausektor, Handwerk")]
        BausektorHandwerk = 1L << 27,

        [Description("Reinigungen")]
        Reinigungen = 1L << 28,

        [Description("Weitere Geschäfte")]
        WeitereGeschaefte = 1L << 29,

        [Description("Wohnausstattung")]
        Wohnausstattung = 1L << 30,
    }

    [Flags]
    public enum PoiTypeDienstleister : long
    {
        [Description("Auto und Motor")]
        AutoundMotor = 1L,

        [Description("Bausektor, Handwerk")]
        BausektorHandwerk = 1L << 1,

        [Description("Kinderbetreuung Animation")]
        KinderbetreuungAnimation = 1L << 2,

        [Description("Werbung und Grafik")]
        WerbungundGrafik = 1L << 3,

        [Description("Reinigung")]
        Reinigung = 1L << 4,

        [Description("Tierpflege")]
        Tierpflege = 1L << 5,

        [Description("Bank, Bankomat")]
        BankBankomat = 1L << 6,

        [Description("Beauty und Wellness")]
        BeautyundWellness = 1L << 7,

        [Description("Friseur")]
        Friseur = 1L << 8,

        [Description("Kosmetik")]
        Kosmetik = 1L << 9,

        [Description("Massage, Heilbäder")]
        MassageHeilbaeder = 1L << 10,

        [Description("Day Spa, Sauna")]
        DaySpaSauna = 1L << 11,

        [Description("Kneippbäder")]
        Kneippbaeder = 1L << 12,

        [Description("Weitere Beauty und Wellness")]
        WeitereBeautyundWellness = 1L << 13,

        [Description("Weitere Dienstleister")]
        WeitereDienstleister = 1L << 14,
    }

    [Flags]
    public enum PoiTypeHandwerk
    {
        [Description("Bildhauer")]
        Bildhauer = 1,

        [Description("Kunstwebereien")]
        Kunstwebereien = 1 << 1,

        [Description("Federkielstickereien")]
        Federkielstickereien = 1 << 2,

        [Description("Kunstmaler und Vergolder")]
        KunstmalerundVergolder = 1 << 3,

        [Description("Weitere Kunsthandwerker")]
        WeitereHandwerker = 1 << 4,

        [Description("Künstler")]
        Kuenstler = 1 << 5,
    }

    [Flags]
    public enum PoiTypeNachtlebenUnterhaltung
    {
        [Description("Biergärten")]
        Biergaerten = 1,

        [Description("Cocktailbars")]
        Cocktailbars = 1 << 1,

        [Description("Diskotheken, Nachtclubs")]
        DiskothekenNachtclubs = 1 << 2,

        [Description("Weitere Nachtleben und Unterhaltungen")]
        WeitereNachtlebenundUnterhaltungen = 1 << 3,
    }

    [Flags]
    public enum PoiTypeOeffentlicheEinrichtungen
    {
        [Description("Bibliotheken")]
        Bibliotheken = 1,

        [Description("Gemeinden")]
        Gemeinden = 1 << 1,

        [Description("Öffentliches WLANs")]
        OeffentlichesWLANs = 1 << 2,

        [Description("Polizei, Carabinieri")]
        PolizeiCarabinieri = 1 << 3,

        [Description("Post")]
        Post = 1 << 4,

        [Description("Recyclinghöfe")]
        Recyclinghoefe = 1 << 5,

        [Description("Infobüro")]
        Infobuero = 1 << 6,

        [Description("Infopoint")]
        Infopoint = 1 << 7,

        [Description("WCs")]
        WCs = 1 << 8,

        [Description("Krankenhaus")]
        Krankenhaus = 1 << 9,

        [Description("Weitere öffentliche Einrichtungen")]
        WeitereoeffentlicheEinrichtungen = 1 << 10,

        [Description("Trinkwasserauffüllpunkte")]
        Trinkwasserauffuellpunkte = 1 << 11,
    }

    [Flags]
    public enum PoiTypeVerkehrTransport
    {
        [Description("Bushaltestellen")]
        Bushaltestellen = 1,

        [Description("Parkplätze")]
        Parkplaetze = 1 << 1,

        [Description("Tankstellen Benzin Diesel")]
        TankstellenBenzinDiesel = 1 << 2,

        [Description("E-Auto Ladestation")]
        ETankstellenLadestationen = 1 << 3,

        [Description("Taxi, Mietwagen mit Fahrer, Bus")]
        TaxiMietwagenmitFahrerBus = 1 << 4,

        [Description("Zugbahnhöfe")]
        Zugbahnhoefe = 1 << 5,

        [Description("Carsharing")]
        Carsharing = 1 << 6,

        [Description("Tankstellen Methan")]
        TankstellenMethan = 1 << 7,

        [Description("Tankstellen Autogas")]
        TankstellenAutogas = 1 << 8,

        [Description("Tankstellen Wasserstoff")]
        TankstellenWasserstoff = 1 << 9,

        [Description("Weitere Verkehr und Transport")]
        WeitereVerkehrundTransport = 1 << 10,

        [Description("E-Bike Ladestation")]
        EBikeLadestation = 1 << 11,

        [Description("Fahrradbox")]
        Fahrradbox = 1 << 12,
    }

    [Flags]
    public enum PoiTypeKulturSehenswuerdigkeiten
    {
        [Description("Denkmäler, Naturdenkmäler")]
        poitype1 = 1,

        [Description("Grünanlagen")]
        poitype2 = 1 << 1,

        [Description("Historische Gebäude")]
        poitype3 = 1 << 2,

        [Description("Ansitze")]
        poitype4 = 1 << 3,

        [Description("Schlösser")]
        poitype5 = 1 << 4,

        [Description("Burgen")]
        poitype6 = 1 << 5,

        [Description("Ruinen")]
        poitype7 = 1 << 6,

        [Description("Weitere historische Gebäude")]
        poitype8 = 1 << 7,

        [Description("Kirchen, Kapellen, Religiöse Zentren")]
        poitype9 = 1 << 8,

        [Description("Kulturzentren")]
        poitype10 = 1 << 9,

        [Description("Museen und Ausstellungen")]
        poitype11 = 1 << 10,

        [Description("Naturparkhaus, Nationalparkhaus")]
        poitype12 = 1 << 11,

        [Description("Produktionsstätten")]
        poitype13 = 1 << 12,

        [Description("Bierbrauerei")]
        poitype14 = 1 << 13,

        [Description("Brennereien")]
        poitype15 = 1 << 14,

        [Description("Obstgenossenschaft")]
        poitype16 = 1 << 15,

        [Description("Kellereien und Winzer")]
        poitype17 = 1 << 16,

        [Description("Weitere Produzenten")]
        poitype18 = 1 << 17,

        [Description("Seen, Wasserfälle")]
        poitype19 = 1 << 18,

        [Description("Theater")]
        poitype20 = 1 << 19,

        [Description("Aussichtspunkte")]
        poitype21 = 1 << 20,

        [Description("Mystische Stätten")]
        poitype22 = 1 << 21,

        [Description("Historische Plätze")]
        poitype23 = 1 << 22,

        [Description("Weitere Kultur und Sehenswürdigkeiten")]
        poitype24 = 1 << 23,
    }

    [Flags]
    public enum PoiTypeGeschaefteDienstleister : long
    {
        [Description("Bank, Bankomat")]
        BankBankomat = 1L,

        [Description("Beauty und Wellness")]
        BeautyundWellness = 1L << 1,

        [Description("Friseur")]
        Friseur = 1L << 2,

        [Description("Kosmetik")]
        Kosmetik = 1L << 3,

        [Description("Massage, Heilbäder")]
        MassageHeilbaeder = 1L << 4,

        [Description("Weitere Beauty und Wellness")]
        WeitereBeautyundWellness = 1L << 5,

        [Description("Geschäfte")]
        Geschaefte = 1L << 6,

        [Description("Getränke")]
        Getraenke = 1L << 7,

        [Description("Antiquitäten")]
        Antiquitaeten = 1L << 8,

        [Description("Blumen")]
        Blumen = 1L << 9,

        [Description("Computerzubehör, Technik")]
        ComputerzubehoerTechnik = 1L << 10,

        [Description("Drogerie")]
        Drogerie = 1L << 11,

        [Description("Fahrräder")]
        Fahrraeder = 1L << 12,

        [Description("Farben, Tapeten")]
        FarbenTapeten = 1L << 13,

        [Description("Fleisch und Wurstwaren")]
        FleischundWurstwaren = 1L << 14,

        [Description("Haushaltswaren")]
        Haushaltswaren = 1L << 15,

        [Description("Juweliere, Goldschmiede")]
        JuweliereGoldschmiede = 1L << 16,

        [Description("Kunsthandwerke")]
        Kunsthandwerke = 1L << 17,

        [Description("Landwirtschaftliche Artikel")]
        LandwirtschaftlicheArtikel = 1L << 18,

        [Description("Lebensmittel")]
        Lebensmittel = 1L << 19,

        [Description("Lederwaren, Schuhe")]
        LederwarenSchuhe = 1L << 20,

        [Description("Lokale traditionelle Produkte")]
        LokaletraditionelleProdukte = 1L << 21,

        [Description("Mode, Bekleidung")]
        ModeBekleidung = 1L << 22,

        [Description("Obst- und Gemüse")]
        ObstundGemüse = 1L << 23,

        [Description("Optiker, Foto")]
        OptikerFoto = 1L << 24,

        [Description("Produktionsstätten")]
        Produktionsstaetten = 1L << 25,

        [Description("Souvenir")]
        Souvenir = 1L << 26,

        [Description("Sportartikel")]
        Sportartikel = 1L << 27,

        [Description("Tierbedarf")]
        Tierbedarf = 1L << 28,

        [Description("Zeitungen, Bücher und Papierwaren")]
        ZeitungenBuecherundPapierwaren = 1L << 29,

        [Description("Weitere Geschäfte")]
        WeitereGeschaefte = 1L << 30,

        [Description("Weitere")]
        Weitere = 1L << 31,

        [Description("Auto und Motor")]
        AutoundMotor = 1L << 32,

        [Description("Bausektor")]
        Bausektor = 1L << 33,

        [Description("Medien")]
        Medien = 1L << 34,

        [Description("Reinigung")]
        Reinigung = 1L << 35,

        [Description("Tierpflege")]
        Tierpflege = 1L << 36,

        [Description("Weitere")]
        WeitereWeitere = 1L << 37,
    }

    [Flags]
    public enum PoiTypeSportFreizeit : long
    {
        [Description("Guides, Clubs, Schulen")]
        GuidesClubsSchulen = 1L,

        [Description("Ski- und Bergführer")]
        SkiundBergfuehrer = 1L << 1,

        [Description("Bikeguides")]
        Bikeguides = 1L << 2,

        [Description("Canyoning, Hydrospeed")]
        Canyoning = 1L << 3,

        [Description("Kajak")]
        Kajak = 1L << 4,

        [Description("Kitesurfen, Windsurfen")]
        KitesurfenWindsurfen = 1L << 5,

        [Description("Paragleiten, Drachenfliegen")]
        ParagleitenDrachenfliegen = 1L << 6,

        [Description("Rafting")]
        Rafting = 1L << 7,

        [Description("Skischulen")]
        Skischulen = 1L << 8,

        [Description("Wanderführer")]
        Wanderfuehrer = 1L << 9,

        [Description("Langlaufschule")]
        Langlaufschule = 1L << 10,

        [Description("Skiclubs")]
        Skiclubs = 1L << 11,

        [Description("Weitere Guides, Clubs, Schulen")]
        WeitereGuidesClubsSchulen = 1L << 12,

        [Description("Spiel- und Sportanlagen")]
        SpielundSportanlagen = 1L << 13,

        [Description("Badeseen")]
        Badeseen = 1L << 14,

        [Description("Bogenschiessanlagen")]
        Bogenschiessanlagen = 1L << 15,

        [Description("Eisklettern")]
        Eisklettern = 1L << 16,

        [Description("Fischergewässer")]
        Fischergewaesser = 1L << 17,

        [Description("Fitnesscenter")]
        Fitnesscenter = 1L << 18,

        [Description("Freibäder")]
        Freibaeder = 1L << 19,

        [Description("Fussballplätze")]
        Fußballplaetze = 1L << 20,

        [Description("Golfplätze")]
        Golfplaetze = 1L << 21,

        [Description("Hallenbäder")]
        Hallenbaeder = 1L << 22,

        [Description("Kinderspielplätze")]
        Kinderspielplaetze = 1L << 23,

        [Description("Klettergarten")]
        Klettergarten = 1L << 24,

        [Description("Kletterhallen")]
        Kletterhallen = 1L << 25,

        [Description("Kletterparks")]
        Kletterparks = 1L << 26,

        [Description("Minigolfplätze")]
        Minigolfplaetze = 1L << 27,

        [Description("Pferdekutschenfahrten")]
        Pferdekutschenfahrten = 1L << 28,

        [Description("Pferdeschlittenfahrten")]
        Pferdeschlittenfahrten = 1L << 29,

        [Description("Radraststätten")]
        Radraststaetten = 1L << 30,

        [Description("Reitställe")]
        Reitstaelle = 1L << 31,

        [Description("Schlittschuhlaufen")]
        Schlittschuhlaufen = 1L << 32,

        [Description("Tennisplätze")]
        Tennisplaetze = 1L << 33,

        [Description("Volleyball, Beachvolleyball")]
        VolleyballBeachvolleyball = 1L << 34,

        [Description("Zip-lines")]
        Ziplines = 1L << 35,

        [Description("Kneippanlage")]
        Kneippanlage = 1L << 36,

        [Description("Langlaufzentren")]
        Langlaufzentren = 1L << 37,

        [Description("Weitere Spiel- und Sportanlagen")]
        WeitereSpielundSportanlagen = 1L << 38,

        [Description("Verleih, Depot")]
        VerleihDepot = 1L << 39,

        [Description("E-Bike-Verleihe")]
        EBikeVerleihe = 1L << 40,

        [Description("Radverleihe")]
        Radverleihe = 1L << 41,

        [Description("Schlittschuhverleihe")]
        Schlittschuhverleihe = 1L << 42,

        [Description("Skidepots")]
        Skidepots = 1L << 43,

        [Description("Skiverleihe")]
        Skiverleihe = 1L << 44,

        [Description("Verleih von Kinderartikeln")]
        VerleihvonKinderartikeln = 1L << 45,

        [Description("Verleih von Langlaufskiern")]
        VerleihLanglaufski = 1L << 46,

        [Description("Weitere Verleih, Depot")]
        WeitereVerleihDepot = 1L << 47,

        [Description("Schneeschuhverleih")]
        Schneeschuhverleih = 1L << 48,

        [Description("Tierpark")]
        Tierpark = 1L << 49,

        [Description("Bikepark")]
        Bikepark = 1L << 50,

        [Description("Pumptracks und mehr")]
        Pumptracksundmehr = 1L << 51,
    }

    [Flags]
    public enum PoiTypeVereine
    {
        [Description("Sport")]
        Sport = 1,

        [Description("Kultur, Musik, Theater")]
        KulturMusikTheater = 1 << 1,

        [Description("Tier, Natur, Wissenschaft")]
        TierNaturWissenschaft = 1 << 2,

        [Description("Gesellschaft, Politisch")]
        GesellschaftPolitisch = 1 << 3,

        [Description("Sozial, Rettung, gemeinnützig")]
        SozialRettungGemeinnuetzig = 1 << 4,

        [Description("Freizeitverein")]
        Freizeitverein = 1 << 5,

        [Description("weitere Vereine")]
        WeitereVereine = 1 << 6,
    }

    [Flags]
    public enum PoiTypeUnternehmen
    {
        [Description("Industrie")]
        Industrie = 1,

        [Description("Produktion")]
        Produktion = 1 << 1,

        [Description("Baugewerbe")]
        Baugewerbe = 1 << 2,

        [Description("Handwerk")]
        Handwerk = 1 << 3,

        [Description("Energie")]
        Energie = 1 << 4,

        [Description("weitere Unternehmen")]
        WeitereUnternehmen = 1 << 5,
    }

    #endregion

    ////Difficulty für Activities POIs
    //[Flags]
    //public enum DifficultyFlag
    //{
    //    leicht = 1,
    //    wenigschwierig = 2,
    //    maessigschwierig = 4,
    //    schwierig = 8,
    //    sehrschwierig = 16
    //}

    ////Difficulty für Activities POIs
    //[Flags]
    //public enum SlopeDifficultyFlag
    //{
    //    blau = 1,
    //    rot = 2,
    //    schwarz = 4
    //}

    ////Difficulty für Activities POIs
    //[Flags]
    //public enum SkitrackDifficultyFlag
    //{
    //    blau = 1,
    //    gelb = 2,
    //    rot = 4,
    //    schwarz = 8
    //}
}
