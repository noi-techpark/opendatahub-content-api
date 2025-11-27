// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DataModel;

namespace Helper
{
    public static class HasLanguageHelper
    {
        private static void FixDetailBaseAndIntroText(Detail mydetail)
        {
            if (!String.IsNullOrEmpty(mydetail.BaseText))
            {
                mydetail.BaseText = mydetail.BaseText.Trim();

                if (mydetail.BaseText == "&#10;&#10;" || mydetail.BaseText == "\n\n")
                    mydetail.BaseText = "";
            }
            if (!String.IsNullOrEmpty(mydetail.IntroText))
            {
                mydetail.IntroText = mydetail.IntroText.Trim();

                if (mydetail.IntroText == "&#10;&#10;" || mydetail.IntroText == "\n\n")
                    mydetail.IntroText = "";
            }
        }

        private static void FixDetailLanguageField(ILanguage mydetail, string language)
        {
            if (String.IsNullOrEmpty(mydetail.Language))
                mydetail.Language = language;
        }

        //Generic
        public static void CheckMyInsertedLanguages<T>(
            this T mypoiactivity,
            List<string>? availablelanguages
        )
            where T : IHasLanguage, IContactInfosAware, IDetailInfosAware
        {
            if (availablelanguages == null || availablelanguages.Count == 0)
                availablelanguages = new List<string>()
                {
                    "de",
                    "it",
                    "en",
                    "nl",
                    "cs",
                    "pl",
                    "fr",
                    "ru",
                };

            if (mypoiactivity.HasLanguage == null)
                mypoiactivity.HasLanguage = new List<string>();

            //Detail, ImageGallery, ContactInfos, AdditionalArticleInfos,
            foreach (string language in availablelanguages)
            {
                if (
                    mypoiactivity.Detail.ContainsKey(language)
                    || mypoiactivity.ContactInfos.ContainsKey(language)
                ) // || mypoiactivity.AdditionalPoiInfos.ContainsKey(language))
                {
                    bool removelang = true;

                    if (
                        mypoiactivity.Detail.ContainsKey(language)
                        && mypoiactivity.Detail[language] != null
                    )
                    {
                        var detailvalues = mypoiactivity.Detail[language];

                        FixDetailLanguageField(detailvalues, language);
                        FixDetailBaseAndIntroText(detailvalues);

                        if (!String.IsNullOrEmpty(detailvalues.AdditionalText))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.BaseText))
                            if (!String.IsNullOrEmpty(detailvalues.BaseText.Trim()))
                                removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.GetThereText))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.Header))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.IntroText))
                            if (!String.IsNullOrEmpty(detailvalues.IntroText.Trim()))
                                removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.SubHeader))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.Title))
                            removelang = false;
                    }

                    if (
                        mypoiactivity.ContactInfos.ContainsKey(language)
                        && mypoiactivity.ContactInfos[language] != null
                    )
                    {
                        var contactvalues = mypoiactivity.ContactInfos[language];

                        FixDetailLanguageField(contactvalues, language);

                        if (!String.IsNullOrEmpty(contactvalues.Address))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.City))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.CompanyName))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.CountryCode))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.CountryName))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Email))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Faxnumber))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Givenname))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.LogoUrl))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.NamePrefix))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Phonenumber))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Surname))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Tax))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Url))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Vat))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.ZipCode))
                            removelang = false;
                    }

                    //Add Language
                    if (removelang == false)
                    {
                        if (!mypoiactivity.HasLanguage.Contains(language))
                            mypoiactivity.HasLanguage.Add(language);
                    }
                    //Remove Language
                    else if (removelang == true)
                    {
                        if (mypoiactivity.Detail.ContainsKey(language))
                            mypoiactivity.Detail.Remove(language);

                        if (mypoiactivity.ContactInfos.ContainsKey(language))
                            mypoiactivity.ContactInfos.Remove(language);

                        if (mypoiactivity.HasLanguage.Contains(language))
                            mypoiactivity.HasLanguage.Remove(language);
                    }
                }
            }
        }

        //For Articles
        public static void CheckMyInsertedLanguages(
            this Article myarticle,
            List<string> availablelanguages
        )
        {
            if (myarticle.HasLanguage == null)
                myarticle.HasLanguage = new List<string>();

            //Detail, ImageGallery, ContactInfos, AdditionalArticleInfos,
            foreach (string language in availablelanguages)
            {
                if (
                    myarticle.Detail.ContainsKey(language)
                    || myarticle.ContactInfos.ContainsKey(language)
                    || myarticle.AdditionalArticleInfos.ContainsKey(language)
                )
                {
                    bool removelang = true;

                    if (
                        myarticle.Detail.ContainsKey(language)
                        && myarticle.Detail[language] != null
                    )
                    {
                        var detailvalues = myarticle.Detail[language];

                        FixDetailLanguageField(detailvalues, language);
                        FixDetailBaseAndIntroText(detailvalues);

                        if (!String.IsNullOrEmpty(detailvalues.AdditionalText))
                            if (!String.IsNullOrEmpty(detailvalues.AdditionalText.Trim()))
                                removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.BaseText))
                            if (!String.IsNullOrEmpty(detailvalues.BaseText.Trim()))
                                removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.GetThereText))
                            if (!String.IsNullOrEmpty(detailvalues.GetThereText.Trim()))
                                removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.Header))
                            if (!String.IsNullOrEmpty(detailvalues.Header.Trim()))
                                removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.IntroText))
                            if (!String.IsNullOrEmpty(detailvalues.IntroText.Trim()))
                                removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.SubHeader))
                            if (!String.IsNullOrEmpty(detailvalues.SubHeader.Trim()))
                                removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.Title))
                            if (!String.IsNullOrEmpty(detailvalues.Title.Trim()))
                                removelang = false;

                        myarticle.Detail[language].Language = language;
                    }

                    if (
                        myarticle.ContactInfos.ContainsKey(language)
                        && myarticle.ContactInfos[language] != null
                    )
                    {
                        var contactvalues = myarticle.ContactInfos[language];

                        FixDetailLanguageField(contactvalues, language);

                        if (!String.IsNullOrEmpty(contactvalues.Address))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.City))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.CompanyName))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.CountryCode))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.CountryName))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Email))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Faxnumber))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Givenname))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.LogoUrl))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.NamePrefix))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Phonenumber))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Surname))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Tax))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Url))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Vat))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.ZipCode))
                            removelang = false;

                        myarticle.ContactInfos[language].Language = language;
                    }

                    if (myarticle.AdditionalArticleInfos.ContainsKey(language))
                    {
                        var additionalvalues = myarticle.AdditionalArticleInfos[language];

                        FixDetailLanguageField(additionalvalues, language);

                        if (additionalvalues.Elements.Count > 0)
                            removelang = false;
                    }

                    //Add Language
                    if (removelang == false)
                    {
                        if (!myarticle.HasLanguage.Contains(language))
                            myarticle.HasLanguage.Add(language);
                    }
                    //Remove Language
                    else if (removelang == true)
                    {
                        if (myarticle.Detail.ContainsKey(language))
                            myarticle.Detail.Remove(language);

                        if (myarticle.ContactInfos.ContainsKey(language))
                            myarticle.ContactInfos.Remove(language);

                        if (myarticle.HasLanguage.Contains(language))
                            myarticle.HasLanguage.Remove(language);
                    }
                }
            }
        }

        //For Activities, Pois, ODHActivityPois
        public static void CheckMyInsertedLanguages(
            this PoiBaseInfos mypoiactivity,
            List<string> availablelanguages
        )
        {
            if (mypoiactivity.HasLanguage == null)
                mypoiactivity.HasLanguage = new List<string>();

            //Detail, ImageGallery, ContactInfos, AdditionalArticleInfos,
            foreach (string language in availablelanguages)
            {
                if (
                    mypoiactivity.Detail.ContainsKey(language)
                    || mypoiactivity.ContactInfos.ContainsKey(language)
                ) // || mypoiactivity.AdditionalPoiInfos.ContainsKey(language))
                {
                    bool removelang = true;

                    if (
                        mypoiactivity.Detail.ContainsKey(language)
                        && mypoiactivity.Detail[language] != null
                    )
                    {
                        var detailvalues = mypoiactivity.Detail[language];

                        FixDetailLanguageField(detailvalues, language);
                        FixDetailBaseAndIntroText(detailvalues);

                        if (!String.IsNullOrEmpty(detailvalues.AdditionalText))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.BaseText))
                            if (!String.IsNullOrEmpty(detailvalues.BaseText.Trim()))
                                removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.GetThereText))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.Header))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.IntroText))
                            if (!String.IsNullOrEmpty(detailvalues.IntroText.Trim()))
                                removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.SubHeader))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.Title))
                            removelang = false;
                    }

                    if (
                        mypoiactivity.ContactInfos.ContainsKey(language)
                        && mypoiactivity.ContactInfos[language] != null
                    )
                    {
                        var contactvalues = mypoiactivity.ContactInfos[language];

                        FixDetailLanguageField(contactvalues, language);

                        if (!String.IsNullOrEmpty(contactvalues.Address))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.City))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.CompanyName))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.CountryCode))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.CountryName))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Email))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Faxnumber))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Givenname))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.LogoUrl))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.NamePrefix))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Phonenumber))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Surname))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Tax))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Url))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Vat))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.ZipCode))
                            removelang = false;
                    }

                    if (mypoiactivity.AdditionalPoiInfos.ContainsKey(language))
                    {
                        var additionalvalues = mypoiactivity.AdditionalPoiInfos[language];

                        FixDetailLanguageField(additionalvalues, language);

                        //Always present do not remove
                        //if (additionalvalues.Elements.Count > 0)
                        //    removelang = false;
                    }

                    //Add Language
                    if (removelang == false)
                    {
                        if (!mypoiactivity.HasLanguage.Contains(language))
                            mypoiactivity.HasLanguage.Add(language);
                    }
                    //Remove Language
                    else if (removelang == true)
                    {
                        if (mypoiactivity.Detail.ContainsKey(language))
                            mypoiactivity.Detail.Remove(language);

                        if (mypoiactivity.ContactInfos.ContainsKey(language))
                            mypoiactivity.ContactInfos.Remove(language);

                        if (mypoiactivity.HasLanguage.Contains(language))
                            mypoiactivity.HasLanguage.Remove(language);
                    }
                }
            }
        }

        //For Events
        public static void CheckMyInsertedLanguages(
            this Event mypoiactivity,
            List<string> availablelanguages
        )
        {
            if (mypoiactivity.HasLanguage == null)
                mypoiactivity.HasLanguage = new List<string>();

            //Detail, ImageGallery, ContactInfos, AdditionalArticleInfos,
            foreach (string language in availablelanguages)
            {
                if (
                    mypoiactivity.Detail.ContainsKey(language)
                    || mypoiactivity.ContactInfos.ContainsKey(language)
                ) // || mypoiactivity.AdditionalPoiInfos.ContainsKey(language))
                {
                    bool removelang = true;

                    if (
                        mypoiactivity.Detail.ContainsKey(language)
                        && mypoiactivity.Detail[language] != null
                    )
                    {
                        var detailvalues = mypoiactivity.Detail[language];

                        FixDetailLanguageField(detailvalues, language);
                        FixDetailBaseAndIntroText(detailvalues);

                        if (!String.IsNullOrEmpty(detailvalues.AdditionalText))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.BaseText))
                            if (!String.IsNullOrEmpty(detailvalues.BaseText.Trim()))
                                removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.GetThereText))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.Header))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.IntroText))
                            if (!String.IsNullOrEmpty(detailvalues.IntroText.Trim()))
                                removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.SubHeader))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.Title))
                            removelang = false;
                    }

                    if (
                        mypoiactivity.ContactInfos.ContainsKey(language)
                        && mypoiactivity.ContactInfos[language] != null
                    )
                    {
                        var contactvalues = mypoiactivity.ContactInfos[language];

                        FixDetailLanguageField(contactvalues, language);

                        if (!String.IsNullOrEmpty(contactvalues.Address))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.City))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.CompanyName))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.CountryCode))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.CountryName))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Email))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Faxnumber))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Givenname))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.LogoUrl))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.NamePrefix))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Phonenumber))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Surname))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Tax))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Url))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Vat))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.ZipCode))
                            removelang = false;
                    }
                    //Add Language
                    if (removelang == false)
                    {
                        if (!mypoiactivity.HasLanguage.Contains(language))
                            mypoiactivity.HasLanguage.Add(language);
                    }
                    //Remove Language
                    else if (removelang == true)
                    {
                        if (mypoiactivity.Detail.ContainsKey(language))
                            mypoiactivity.Detail.Remove(language);

                        if (mypoiactivity.ContactInfos.ContainsKey(language))
                            mypoiactivity.ContactInfos.Remove(language);

                        if (mypoiactivity.HasLanguage.Contains(language))
                            mypoiactivity.HasLanguage.Remove(language);
                    }
                }
            }
        }

        ////For Gastronomy
        //public static void CheckMyInsertedLanguages(
        //    this Gastronomy mypoiactivity,
        //    List<string> availablelanguages
        //)
        //{
        //    if (mypoiactivity.HasLanguage == null)
        //        mypoiactivity.HasLanguage = new List<string>();

        //    //Detail, ImageGallery, ContactInfos, AdditionalArticleInfos,
        //    foreach (string language in availablelanguages)
        //    {
        //        if (
        //            mypoiactivity.Detail.ContainsKey(language)
        //            || mypoiactivity.ContactInfos.ContainsKey(language)
        //        ) // || mypoiactivity.AdditionalPoiInfos.ContainsKey(language))
        //        {
        //            bool removelang = true;

        //            if (
        //                mypoiactivity.Detail.ContainsKey(language)
        //                && mypoiactivity.Detail[language] != null
        //            )
        //            {
        //                var detailvalues = mypoiactivity.Detail[language];
        //                FixDetailBaseAndIntroText(detailvalues);

        //                FixDetailLanguageField(detailvalues, language);

        //                if (!String.IsNullOrEmpty(detailvalues.AdditionalText))
        //                    removelang = false;
        //                if (!String.IsNullOrEmpty(detailvalues.BaseText))
        //                    if (!String.IsNullOrEmpty(detailvalues.BaseText.Trim()))
        //                        removelang = false;
        //                if (!String.IsNullOrEmpty(detailvalues.GetThereText))
        //                    removelang = false;
        //                if (!String.IsNullOrEmpty(detailvalues.Header))
        //                    removelang = false;
        //                if (!String.IsNullOrEmpty(detailvalues.IntroText))
        //                    if (!String.IsNullOrEmpty(detailvalues.IntroText.Trim()))
        //                        removelang = false;
        //                if (!String.IsNullOrEmpty(detailvalues.SubHeader))
        //                    removelang = false;
        //                if (!String.IsNullOrEmpty(detailvalues.Title))
        //                    removelang = false;
        //            }

        //            if (
        //                mypoiactivity.ContactInfos.ContainsKey(language)
        //                && mypoiactivity.ContactInfos[language] != null
        //            )
        //            {
        //                var contactvalues = mypoiactivity.ContactInfos[language];

        //                FixDetailLanguageField(contactvalues, language);

        //                if (!String.IsNullOrEmpty(contactvalues.Address))
        //                    removelang = false;
        //                if (!String.IsNullOrEmpty(contactvalues.City))
        //                    removelang = false;
        //                if (!String.IsNullOrEmpty(contactvalues.CompanyName))
        //                    removelang = false;
        //                if (!String.IsNullOrEmpty(contactvalues.CountryCode))
        //                    removelang = false;
        //                if (!String.IsNullOrEmpty(contactvalues.CountryName))
        //                    removelang = false;
        //                if (!String.IsNullOrEmpty(contactvalues.Email))
        //                    removelang = false;
        //                if (!String.IsNullOrEmpty(contactvalues.Faxnumber))
        //                    removelang = false;
        //                if (!String.IsNullOrEmpty(contactvalues.Givenname))
        //                    removelang = false;
        //                if (!String.IsNullOrEmpty(contactvalues.LogoUrl))
        //                    removelang = false;
        //                if (!String.IsNullOrEmpty(contactvalues.NamePrefix))
        //                    removelang = false;
        //                if (!String.IsNullOrEmpty(contactvalues.Phonenumber))
        //                    removelang = false;
        //                if (!String.IsNullOrEmpty(contactvalues.Surname))
        //                    removelang = false;
        //                if (!String.IsNullOrEmpty(contactvalues.Tax))
        //                    removelang = false;
        //                if (!String.IsNullOrEmpty(contactvalues.Url))
        //                    removelang = false;
        //                if (!String.IsNullOrEmpty(contactvalues.Vat))
        //                    removelang = false;
        //                if (!String.IsNullOrEmpty(contactvalues.ZipCode))
        //                    removelang = false;
        //            }

        //            //Add Language
        //            if (removelang == false)
        //            {
        //                if (!mypoiactivity.HasLanguage.Contains(language))
        //                    mypoiactivity.HasLanguage.Add(language);
        //            }
        //            //Remove Language
        //            else if (removelang == true)
        //            {
        //                if (mypoiactivity.Detail.ContainsKey(language))
        //                    mypoiactivity.Detail.Remove(language);

        //                if (mypoiactivity.ContactInfos.ContainsKey(language))
        //                    mypoiactivity.ContactInfos.Remove(language);

        //                if (mypoiactivity.HasLanguage.Contains(language))
        //                    mypoiactivity.HasLanguage.Remove(language);
        //            }
        //        }
        //    }
        //}

        //For EventShort
        public static void CheckMyInsertedLanguages(
            this EventShort mydata,
            List<string>? availablelanguages = null
        )
        {
            if (availablelanguages == null)
                availablelanguages = new List<string> { "de", "it", "en" };

            if (mydata.HasLanguage == null)
                mydata.HasLanguage = new List<string>();

            foreach (string language in availablelanguages)
            {
                if (
                    mydata.EventText.ContainsKey(language)
                    || mydata.EventTitle.ContainsKey(language)
                ) // || mypoiactivity.AdditionalPoiInfos.ContainsKey(language))
                {
                    bool removelang = true;

                    if (
                        mydata.EventText.ContainsKey(language)
                        && mydata.EventText[language] != null
                    )
                    {
                        if (!String.IsNullOrEmpty(mydata.EventText[language]))
                            removelang = false;
                    }

                    if (
                        mydata.EventTitle.ContainsKey(language)
                        && mydata.EventTitle[language] != null
                    )
                    {
                        if (!String.IsNullOrEmpty(mydata.EventTitle[language]))
                            removelang = false;
                    }

                    //Add Language
                    if (removelang == false)
                    {
                        if (!mydata.HasLanguage.Contains(language))
                            mydata.HasLanguage.Add(language);
                    }
                    //Remove Language
                    else if (removelang == true)
                    {
                        if (mydata.HasLanguage.Contains(language))
                            mydata.HasLanguage.Remove(language);
                    }
                }
            }
        }

        //For Accommodations
        public static void CheckMyInsertedLanguages(
            this Accommodation mypoiactivity,
            List<string> availablelanguages
        )
        {
            if (mypoiactivity.HasLanguage == null)
                mypoiactivity.HasLanguage = new List<string>();

            //Detail, ImageGallery, ContactInfos, AdditionalArticleInfos,
            foreach (string language in availablelanguages)
            {
                if (
                    mypoiactivity.AccoDetail != null
                    && mypoiactivity.AccoDetail.ContainsKey(language)
                ) // || mypoiactivity.AdditionalPoiInfos.ContainsKey(language))
                {
                    bool removelang = true;

                    if (
                        mypoiactivity.AccoDetail.ContainsKey(language)
                        && mypoiactivity.AccoDetail[language] != null
                    )
                    {
                        var detailvalues = mypoiactivity.AccoDetail[language];

                        FixDetailLanguageField(detailvalues, language);
                        //FixDetailBaseAndIntroText(detailvalues);

                        if (!String.IsNullOrEmpty(detailvalues.Name))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.Lastname))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.Firstname))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.Email))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.Vat))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.City))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.NameAddition))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.Street))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.Website))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.Phone))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.Mobile))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.Fax))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.Zip))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.CountryCode))
                            removelang = false;

                        if (!String.IsNullOrEmpty(detailvalues.Longdesc))
                            if (!String.IsNullOrEmpty(detailvalues.Longdesc.Trim()))
                                removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.Shortdesc))
                            if (!String.IsNullOrEmpty(detailvalues.Shortdesc.Trim()))
                                removelang = false;
                    }

                    //Add Language
                    if (removelang == false)
                    {
                        if (!mypoiactivity.HasLanguage.Contains(language))
                            mypoiactivity.HasLanguage.Add(language);
                    }
                    //Remove Language
                    else if (removelang == true)
                    {
                        if (mypoiactivity.AccoDetail.ContainsKey(language))
                            mypoiactivity.AccoDetail.Remove(language);

                        if (mypoiactivity.HasLanguage.Contains(language))
                            mypoiactivity.HasLanguage.Remove(language);
                    }
                }
            }
        }

        //For AccommodationRooms
        public static void CheckMyInsertedLanguages(
            this AccoRoom mypoiactivity,
            List<string> availablelanguages
        )
        {
            if (mypoiactivity.HasLanguage == null)
                mypoiactivity.HasLanguage = new List<string>();

            //Detail, ImageGallery, ContactInfos, AdditionalArticleInfos,
            foreach (string language in availablelanguages)
            {
                if (
                    mypoiactivity.AccoRoomDetail != null
                    && mypoiactivity.AccoRoomDetail.ContainsKey(language)
                ) // || mypoiactivity.AdditionalPoiInfos.ContainsKey(language))
                {
                    bool removelang = true;

                    if (
                        mypoiactivity.AccoRoomDetail.ContainsKey(language)
                        && mypoiactivity.AccoRoomDetail[language] != null
                    )
                    {
                        var detailvalues = mypoiactivity.AccoRoomDetail[language];

                        FixDetailLanguageField(detailvalues, language);
                        //FixDetailBaseAndIntroText(detailvalues);

                        if (!String.IsNullOrEmpty(detailvalues.Name))
                            removelang = false;

                        if (!String.IsNullOrEmpty(detailvalues.Longdesc))
                            if (!String.IsNullOrEmpty(detailvalues.Longdesc.Trim()))
                                removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.Shortdesc))
                            if (!String.IsNullOrEmpty(detailvalues.Shortdesc.Trim()))
                                removelang = false;
                    }

                    //Add Language
                    if (removelang == false)
                    {
                        if (!mypoiactivity.HasLanguage.Contains(language))
                            mypoiactivity.HasLanguage.Add(language);
                    }
                    //Remove Language
                    else if (removelang == true)
                    {
                        if (mypoiactivity.AccoRoomDetail.ContainsKey(language))
                            mypoiactivity.AccoRoomDetail.Remove(language);

                        if (mypoiactivity.HasLanguage.Contains(language))
                            mypoiactivity.HasLanguage.Remove(language);
                    }
                }
            }
        }

        //For Webcams
        public static void CheckMyInsertedLanguages(
            this WebcamInfo mypoiactivity,
            List<string>? availablelanguages = null
        )
        {
            if (availablelanguages == null)
                availablelanguages = new List<string> { "de", "it", "en" };

            if (mypoiactivity.HasLanguage == null)
                mypoiactivity.HasLanguage = new List<string>();

            //Detail, ImageGallery, ContactInfos, AdditionalArticleInfos,
            foreach (string language in availablelanguages)
            {
                if (
                    mypoiactivity.Detail.ContainsKey(language)
                    || mypoiactivity.ContactInfos.ContainsKey(language)
                ) // || mypoiactivity.AdditionalPoiInfos.ContainsKey(language))
                {
                    bool removelang = true;

                    if (
                        mypoiactivity.Detail.ContainsKey(language)
                        && mypoiactivity.Detail[language] != null
                    )
                    {
                        var detailvalues = mypoiactivity.Detail[language];
                        FixDetailBaseAndIntroText(detailvalues);

                        FixDetailLanguageField(detailvalues, language);

                        if (!String.IsNullOrEmpty(detailvalues.AdditionalText))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.BaseText))
                            if (!String.IsNullOrEmpty(detailvalues.BaseText.Trim()))
                                removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.GetThereText))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.Header))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.IntroText))
                            if (!String.IsNullOrEmpty(detailvalues.IntroText.Trim()))
                                removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.SubHeader))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.Title))
                            removelang = false;
                    }

                    if (
                        mypoiactivity.ContactInfos.ContainsKey(language)
                        && mypoiactivity.ContactInfos[language] != null
                    )
                    {
                        var contactvalues = mypoiactivity.ContactInfos[language];

                        FixDetailLanguageField(contactvalues, language);

                        if (!String.IsNullOrEmpty(contactvalues.Address))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.City))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.CompanyName))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.CountryCode))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.CountryName))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Email))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Faxnumber))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Givenname))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.LogoUrl))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.NamePrefix))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Phonenumber))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Surname))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Tax))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Url))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.Vat))
                            removelang = false;
                        if (!String.IsNullOrEmpty(contactvalues.ZipCode))
                            removelang = false;
                    }

                    //Add Language
                    if (removelang == false)
                    {
                        if (!mypoiactivity.HasLanguage.Contains(language))
                            mypoiactivity.HasLanguage.Add(language);
                    }
                    //Remove Language
                    else if (removelang == true)
                    {
                        if (mypoiactivity.Detail.ContainsKey(language))
                            mypoiactivity.Detail.Remove(language);

                        if (mypoiactivity.ContactInfos.ContainsKey(language))
                            mypoiactivity.ContactInfos.Remove(language);

                        if (mypoiactivity.HasLanguage.Contains(language))
                            mypoiactivity.HasLanguage.Remove(language);
                    }
                }
            }
        }

        //For Wines
        public static void CheckMyInsertedLanguages(
            this Wine mydata,
            List<string> availablelanguages
        )
        {
            if (availablelanguages == null)
                availablelanguages = new List<string> { "de", "it", "en" };

            if (mydata.HasLanguage == null)
                mydata.HasLanguage = new List<string>();

            //Detail, ImageGallery, ContactInfos, AdditionalArticleInfos,
            foreach (string language in availablelanguages)
            {
                if (mydata.Detail.ContainsKey(language))
                {
                    bool removelang = true;

                    if (mydata.Detail.ContainsKey(language) && mydata.Detail[language] != null)
                    {
                        var detailvalues = mydata.Detail[language];

                        FixDetailLanguageField(detailvalues, language);
                        FixDetailBaseAndIntroText(detailvalues);

                        if (!String.IsNullOrEmpty(detailvalues.AdditionalText))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.BaseText))
                            if (!String.IsNullOrEmpty(detailvalues.BaseText.Trim()))
                                removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.GetThereText))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.Header))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.IntroText))
                            if (!String.IsNullOrEmpty(detailvalues.IntroText.Trim()))
                                removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.SubHeader))
                            removelang = false;
                        if (!String.IsNullOrEmpty(detailvalues.Title))
                            removelang = false;
                    }

                    //Add Language
                    if (removelang == false)
                    {
                        if (!mydata.HasLanguage.Contains(language))
                            mydata.HasLanguage.Add(language);
                    }
                    //Remove Language
                    else if (removelang == true)
                    {
                        if (mydata.Detail.ContainsKey(language))
                            mydata.Detail.Remove(language);

                        if (mydata.HasLanguage.Contains(language))
                            mydata.HasLanguage.Remove(language);
                    }
                }
            }
        }
    }
}
