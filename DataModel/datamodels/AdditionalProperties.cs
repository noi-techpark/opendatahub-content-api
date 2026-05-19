// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel.Annotations;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModel
{
    //All AdditionalProperties are found here


    #region ODHActivityPoi AdditionalProperties

    //To check if this is added as AdditionalProperty
    //ODHActivityPoiPropertiesLTSActivity
    //ODHActivityPoiPropertiesLTSPoi
    //ODHActivityPoiPropertiesLTSGastronomy
    //ODHActivityPoiPropertiesSuedtirolWein
    //ODHActivityPoiPropertiesEcharging

    public class PoiAgeDataProperties
    {
        public int? AgeFrom { get; set; }
        public int? AgeTo { get; set; }
    }

    //LTS
    public class ActivityLtsDataProperties
    {
        public double? AltitudeDifference { get; set; }
        public double? AltitudeHighestPoint { get; set; }
        public double? AltitudeLowestPoint { get; set; }
        public double? AltitudeSumUp { get; set; }
        public double? AltitudeSumDown { get; set; }

        public double? DistanceDuration { get; set; }
        public double? DistanceLength { get; set; }

        public bool? IsOpen { get; set; }
        public bool? IsPrepared { get; set; }
        public bool? RunToValley { get; set; }
        public bool? IsWithLigth { get; set; }
        public bool? HasRentals { get; set; }
        public bool? LiftAvailable { get; set; }
        public bool? FeetClimb { get; set; }

        public bool? BikeTransport { get; set; }

        public Ratings? Ratings { get; set; }
        public ICollection<string>? Exposition { get; set; }

        public int? WayNumber { get; set; }
        public string? Number { get; set; }

        public bool? MountainBikePermitted { get; set; }

        public string? LiftType { get; set; }
        public string? LiftCapacityType { get; set; }


        //rating.viaFerrataTechnique, rating.scaleUIAATechnique, rating.singletrackScale,
        //liftPointCard.pointsSingleTripUp, liftPointCard.pointsSingleTripDown
        //minRopeLength, quantityQuickDraws, snowType, snowPark.hasPipe, snowPark.linesNumber, snowPark.jumpsNumber, 
        //snowPark.isInground, snowPark.hasArtificiallySnow, snowPark.hasBoarderCross
    }

    public class PoiLtsDataProperties
    {
        public bool? IsOpen { get; set; }
        public bool? HasFreeEntrance { get; set; }
    }

    public class GastronomyLtsDataProperties
    {
        public int? MaxSeatingCapacity { get; set; }
        public ICollection<CategoryCodes>? CategoryCodes { get; set; }
        public ICollection<DishRates>? DishRates { get; set; }
        public ICollection<CapacityCeremony>? CapacityCeremony { get; set; }
        public ICollection<Facilities>? Facilities { get; set; }
    }

    public class SuedtirolWeinCompanyDataProperties
    {

        //TODO add Dictionary where it needs localization, categorize (socials) etc...

        public IDictionary<string, string>? OpeningtimesWineshop { get; set; }
        public IDictionary<string, string>? OpeningtimesGuides { get; set; }
        public IDictionary<string, string>? OpeningtimesGastronomie { get; set; }
        public IDictionary<string, string>? CompanyHoliday { get; set; }
        public bool? HasVisits { get; set; }
        public bool? HasOvernights { get; set; }
        public bool? HasBiowine { get; set; }
        public bool? HasAccommodation { get; set; }
        public bool? IsVinumHotel { get; set; }
        public bool? IsAnteprima { get; set; }
        public bool? IsWineStories { get; set; }
        public bool? IsWineSummit { get; set; }
        public bool? IsSparklingWineassociation { get; set; }
        public bool? IsWinery { get; set; }
        public bool? HasOnlineshop { get; set; }
        public bool? HasDeliveryservice { get; set; }
        public bool? HasDirectSales { get; set; }
        public bool? IsSkyalpsPartner { get; set; }

        public ICollection<string>? Wines { get; set; }


        public IDictionary<string, string>? OnlineShopurl { get; set; }
        public IDictionary<string, string>? DeliveryServiceUrl { get; set; }
        public IDictionary<string, string>? H1 { get; set; }
        public IDictionary<string, string>? H2 { get; set; }
        public IDictionary<string, string>? Quote { get; set; }
        public IDictionary<string, string>? QuoteAuthor { get; set; }
        public IDictionary<string, string>? DescriptionSparklingWineproducer { get; set; }
        public IDictionary<string, string>? H1SparklingWineproducer { get; set; }
        public IDictionary<string, string>? H2SparklingWineproducer { get; set; }

        public string? ImageSparklingWineproducer { get; set; }
        public string? SocialsFacebook { get; set; }
        public string? SocialsInstagram { get; set; }
        public string? SocialsLinkedIn { get; set; }
        public string? SocialsPinterest { get; set; }
        public string? SocialsTiktok { get; set; }
        public string? SocialsYoutube { get; set; }
        public string? SocialsTwitter { get; set; }
    }

    public class SiagMuseumDataProperties
    {
        //Check if Dictionary outside is supported
        public IDictionary<string, string> OpeningTimes { get; set; }
        public IDictionary<string, string> Entry { get; set; }
        public IDictionary<string, string> Supporter { get; set; }
    }

    public class StaVendingPointsDataProperties
    {
        public bool? HasWebsite { get; set; }
        public bool? SuedtirolPassServices { get; set; }
        public bool? SuedtirolpassOver65apply { get; set; }
        public bool? Duplicate { get; set; }
        public bool? ChargeCard { get; set; }
        public bool? CityCardBus { get; set; }
        public bool? MobileCard { get; set; }
        public bool? BikeMobileCard { get; set; }
        public bool? MuseumMobileCard { get; set; }
    }

    //Independent
    public class EchargingDataProperties
    {
        //Mobility Provides
        //state (ACTIVE)
        //capacity (integer)
        //provider
        //accessInfo (FREE_PUBLICLY_ACCESSIBLE)
        //accessType (PUBLIC)
        //reservable (true/false)
        //paymentInfo
        //outlets [ id, maxPower, maxCurrent, minCurrent, outletTypeCode (Type2Mennekes, CHAdeMO, CCS, 700 bar small vehicles, )  ]

        public int? Capacity { get; set; }

        [SwaggerSchema(Description = "State of the E-chargingstation", ReadOnly = true)]
        [SwaggerEnum(
            new[]
            {
                "UNAVAILABLE",
                "ACTIVE",
                "TEMPORARYUNAVAILABLE",
                "AVAILABLE",
                "UNKNOWN",
                "FAULT",
                "PLANNED",
            }
        )]
        public string? State { get; set; }

        [SwaggerSchema(Description = "Information about Payment", ReadOnly = true)]
        public string? PaymentInfo { get; set; }

        [SwaggerSchema(Description = "Public or private access", ReadOnly = true)]
        [SwaggerEnum(new[] { "PUBLIC", "PRIVATE", "PRIVATE_WITHPUBLICACCESS" })]
        public string? AccessType { get; set; }

        [SwaggerSchema(Description = "Types of the Charging Pistols", ReadOnly = true)]
        [SwaggerEnum(
            new[]
            {
                "Typ 1-Stecker",
                "Typ 2-Stecker",
                "Combo-Stecker",
                "CHAdeMO-Stecker",
                "Tesla Supercharger",
            }
        )]
        public List<string>? ChargingPistolTypes { get; set; }

        [SwaggerSchema(Description = "AccessType Information", ReadOnly = true)]
        public string AccessTypeInfo { get; set; }

        public DateTime? SurveyDate { get; set; }
        public string? SurveyType { get; set; }

        public IDictionary<string, string> SurveyAnnotations { get; set; }

        public bool? Covered { get; set; }
        public bool? VerticalRoadSign { get; set; }
        public bool? HorizontalFloorRoadSign { get; set; }

        [SwaggerSchema(Description = "Charging Station Accessible", ReadOnly = true)]
        public bool? ChargingStationAccessible { get; set; }

        [SwaggerSchema("Maximum operation height in cm")]
        public int? DisplayOrCardReaderOperationHeight { get; set; }

        [SwaggerSchema("Maximum operation height in cm (barrierfree = 90-120 cm)")]
        public int? ChargingPistolOperationHeight { get; set; }

        public int? ChargingCableLength { get; set; }

        public bool? ShieldingPostInFrontOfStation { get; set; }

        [SwaggerSchema(
            "Stufenlose Gehsteiganbindung: zulässige maximale Steigung <5-8%) bodengleich an den Gehsteig angebunden"
        )]
        public bool? SteplessSidewalkConnection { get; set; }

        [SwaggerEnum(new[] { "Barrierefrei", "Bedingt zugänglich", "Nicht zugänglich" })]
        public string? Barrierfree { get; set; }

        //public ICollection<CarparkingArea> CarparkingArea { get; set; }

        public EchargingCarparkingArea? CarParkingSpaceNextToEachOther { get; set; }
        public EchargingCarparkingArea? CarParkingSpaceBehindEachOther { get; set; }
    }

    public class EchargingCarparkingArea
    {
        //[SwaggerEnum(new[] { "column", "row" })]
        //public string? Type { get; set; }

        [SwaggerSchema("Eben (wenn Steigung <5% und Querneigung <3%)")]
        public bool? Flat { get; set; }

        [SwaggerSchema("Steigung % (wenn Steigung >5%)")]
        public double? Gradient { get; set; }

        [SwaggerSchema("Querneigung % (wenn Querneigung >3%)")]
        public double? LateralInclination { get; set; }

        [SwaggerEnum(new[] { "Barrierefrei", "Bedingt zugänglich", "Nicht zugänglich" })]
        public string Pavement { get; set; }

        [SwaggerSchema("Width, (on column barrierfree = 350 cm), (on row barrierfree = 250 cm)")]
        public int? Width { get; set; }

        [SwaggerSchema("Length, (on column barrierfree = 500 cm), (on row barrierfree = 650 cm)")]
        public int? Length { get; set; }

        [SwaggerSchema("Barrier-free access space signage present")]
        public bool? ManeuvringSpaceSignagePresent { get; set; }

        [SwaggerSchema("Barrier-free access space to charging point(monitor / pistol)")]
        public bool? BarrierFreeAccessSpacetoChargingPoint { get; set; }
    }

    #endregion

    #region Event AdditionalProperties

    //AdditionalInfos Centrotrevi
    public class EventCentroTreviDataProperties
    {
        public double Price { get; set; }
        public bool Ticket { get; set; }
        public string TicketInfo { get; set; }
    }

    //LTS Specific
    public class EventLTSDataProperties
    {
        public EventPublisher EventPublisher { get; set; }
        public bool SignOn { get; set; }
        public EventBooking EventBooking { get; set; }
        public EventPrice EventPrice { get; set; }
    }

    //EventShort Specific
    public class EventEuracNoiDataProperties
    {
        public bool? ExternalOrganizer { get; set; }
        public bool? SoldOut { get; set; }
        public AgeRange? TypicalAgeRange { get; set; }
        public string EventLocation { get; set; }
    }

    #endregion
}
