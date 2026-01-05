using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace Reports
{
    public partial class Data
    {
        public Data()
        {
            InitializeComponent();
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            var name = Name.Text.Trim();
            var date = Date.Text.Trim();
            var toggle = ToggleOption.IsChecked == true ? "goto" : "autotel";
            // createdon@OData.Community.Display.V1.FormattedValue
            // address2_telephone1
            // address1_city
            // address1_line1
            // address2_line1
            // address1_postofficebox
            // address1_postalcode
            // c2g_idno
            // name
            // emailaddress1
            // c2g_memberbo@OData.Community.Display.V1.FormattedValue
            // accountid
            // requestURL: https://goto.crm4.dynamics.com/api/data/v9.0/accounts(8511080e-b625-f011-8c4d-6045bdf62a53)
            // Downloads folder
            // https://goto.crm4.dynamics.com/api/data/v9.0/incidents
            //             :authority
            // goto.crm4.dynamics.com
            // :method
            // POST
            // :path
            // /api/data/v9.0/incidents
            // :scheme
            // https
            // accept
            // application/json
            // accept-encoding
            // gzip, deflate, br, zstd
            // accept-language
            // en,he;q=0.9
            // clienthost
            // Browser
            // content-length
            // 3628
            // content-type
            // application/json
            // cookie
            // ReqClientId=5dfad7dd-0f5f-4e5f-b6de-e2712ec48cc0; orgId=81d076e2-542d-4d02-b97e-8aa0cb97a937; persistentSearchTypeCookieEx=1; persistentSearchTypeCookie=0; MSFPC=GUID=a5a91131f77343b3945fe9d4b2e80f42&HASH=a5a9&LV=202506&V=4&LU=1750277707116; CrmOwinAuth=chunks:2; testcookie=1; persistentNavTourCookie_20ea7bcb-9511-eb11-a813-000d3a2d5b22=true; lastUsedApp=appId=0f6cc0bf-abf5-e811-a953-000d3a464508&userId=3cace5de-1be2-ef11-a731-0022488814a3; CrmOwinAuthC1=MAAAADngvaboyxHwhysADTrYjpRH1VH1eYyVTO3iOIjVQSBoL_Wvehaf-w6fsmX8FZbh-R-LCAAAAAAABADtXOuO40Z27naPL2uvnV0sYAQIDAwQwz8yoZoskiJpZLBLUtSdulCiJOoPUSSL95tIShT5GPmRF8mfvEVeIg8SUj0z9kyMrNet9Xq8ze4mSlXF75z66pxTVQJP393c3HwxL71YTJLAQ_yxcG9vbm8-v725uYNH62v84WKw9v5wM1-XXl-3Ny_dokjzb-_v8yLvNGBWUuadGBX3FgB0FzEUxiKTwCjGIDCICBZjGZLp0hAwiDXub2_uvDx_LMZjn38GTbO4xf-lhWlQyrLslGQnyZx7gOPE_U6erkwXRfCfvbhADspI8FiJ374SlV9w807kmVmSJ3bRMZPo3gyhF-X3sJmROEKFm1h5hmyUodhE-V1aWo8f8m9acL3wIvQpwXQZ0CVwivkZCfjI1F2Yu1-O2CNBpbk1g1NQDw4lQBzVdx4_vt-bME1Cz8r1EBbIaAC-hiQwaNMiMQRsHKOsxpRZo8tiBjAt2wYUCZkrEPuBaf7PrRgJyKomvqDtXH76ws268x4t0g4PmFMmeUMDsf5E628KaT4q7tdr1O9Cz_fsFaNmL16w93ux9lIN5tb2uNmE59m6u4LavcPOawIO9ytQjA08O0VcRfRY1oV1JUbboVOzh0WxTO8Nyz6rq15knawBFxsDztO25VEDXDFwRxNpLjGLrG6GSSziqWQAJhyW3ZEnOcnSy0bs0q3WR9XD9yN6e1Rtje-NRGWjUrw6GotLYVGe19bAokePp-rlO05wjsI8genF7sq8NT36vvn1LBQXXlG9dov8mMUwQh8LMIugGTxejz_-ND0c74TiVpOPRnkGUfh4RT70rKJKnx1zlD0e7CMvhZaV_ZYkOgRNdahuh2Qfj_ppmDherLtNIPiP23lH9BRv1tNqzRfcvUhEe1-Jpmsl0IBUzHtyOV_hhFy77n4wIvb-JpqB5Wo8WFayr4L92sSnazfUaqmY9SxPXhHufi1R07VDyrUJZlupmgGV0pr4B3ezSttu8mk0ds2IcDVQ8HvA4QawcgOMXUOkfQPgztx5-fIK60E7q188zOrzq5nZu-b-dsx_17oSw0dm8VBreyj72qC6pIFDC7Ntym7ClwkxlsQ5jCZZaBm0TdvXCF-fJnGaoUjPPQtbYQRGY4DAWMACANhmK0ACost1aboJmySDE1zzKMdggOO6V2A9PXrW7wgcJxuPoyjAkD0J0FeIyJnLER1-xfLWgArxfDDPVXQcjExyD8eYV5r866t8UxJzflWeeL5zhQ1OQ2W7l7IZgoSYBU0DwxmbwEgasRhnsTjO4gQB7Ssw-Nvcc-LGO_OiWfM-8-IgTso4LsorGO9b0M-CKPcej9n7aXG3dc7v_OJFZtu4Z0_n2kJj-rsZbaJicYL8WDvXKa84WW-AnQa5NtvR0uNV_kOBYhgXeoYcL2nYMJMUfSCpjwem_6Lg8KBFY1c_Bv3xyt3efOVdgmHHeAiGf3KSInHCxIBhq9_jJXA_zRSOafzX1uzuhLI7onOFk86zstmOfm0wnG3YlIWRyOYwqstyGEtQJMZ0DYKjEEvTOPd4WR-fo1w3IbolrrCRuGCZd-LiCmC_a8FsLyxQpjdPo_MHFPN41E9a1Cg1vY8AzTXG8jMeZC5MN5Kfkd2fVe5lVjLrC7xDgWmoCYpH8CZ_JcvJ8-galnOZljL3imcE-7OSc3emi39amDuOHewogK-JXTcXekHtLqODU8pqG9KaXoFn_fleXyaNAh0zizrtkblIgiYEVSn6gW9Obm-YH32sb8r5d4f6a5D9IUxT7-f61uaTizBoZrfg8WDgL1r7Lmvuv98JLfWxk_NpmjR2EzWdOgqC1jbzCtThw_C5CEMUWzDLL_WdlQszZL1Te-n9Q3VvurswjlEoozyHDnpAuoC_Xb9qnm_rio6YIdhCtuW296X0Vre-F6LvS3_n8wV-kCXH9J26UWwnzRpXNBuPRZYU7emgKSWhZ1YPkmTohd_DbT8ayXmFiqIl6qHPPA69GMkIfVf30HuRobw1xe-GuGqq39VLbY6mD0BvSgLMPbNtfLwhMC3oZeiDDKZuMy2h_mOOPc-K7IgePPAfeL53QVgjGI2s_Oah-vdvnLg9XLf---qU3f784_9pbCbXClEmyG8sUWxaWz9vbdG8zEGnDQBtfav0a6kyigyUrRuI4QPEq7jwEwf2oQ3D_P8f2S8Q-s9Px9-V0k90PCn9a-PjfUR-L4l-UvoJ-gn6CfoX6efvZQR5L5V-L_l4YvpXMIlPSj9B_wqhn2zvieonpX8ByO-nTb-Xk_h-8vEE_QT95DG_EOgnqp9W3CfoXwT0k4E88fHrg34vZ_F9XAWeiH7_6fjbQn_Wvt0JzUK3zeKOfH6FTI2_orJ3HiwumY40QdEM_aPfRe1Sj34XNTbsv5FkdE5fSebYvyC78wqSoZf81wc8WvKDexa2CTXhYptkERGZ9wRBq_4pNulNfx1OKblLQIUJ_TFJzzlb3gaMhe_T_mDuzYbaviCT1SGZH9ajerAY0VbBzEn_xRbG9_VpWdHdYc7KWu13-VNXixYT5-BYM0liUeII9dTl5sdw2_UTtKReZC_yrTVcRsXYjhezvn1g8GLN29tllYbCbCxU6SF2p4x4dpSEEFgwmh-Kwk7xwxFsd4WzlsJsXvVEHs04d0IVSTBm8Zqq3ZEkD0ueOitT4JDrkSbXQTkb8gG3HycvXsyyE8NO6SnuTPzKLiKOVW1-yRrKqigChbdj-b5eFCsGSYfYOSxgcQi0KySw3R0L70snS5b6qKsB_DAomHOhZyLLX-sNcbuw_jRXAFONJN3YWCppc-koixE9X-4m52wzq_a8D_WK7dflBNeE_W5TGaQQWmBTWyIRmICor5DF3KriWRkKnxHU8yu8RH2JafnReBXTrpAw2apoVbGeFf_9-U_OQhOWvMCXUlvX4-e1Lix7uI5VS_ucr_VpQCvEZK8gjT4U1XRjwGBiHJOpkPfggrHUkX9Gh_M-98oDWKM4dZeInuZSKBPkqbfNAbNFy0NEEaK42CR8_-xbK3EgCh4w1LM5HMo0HWuTibWphwcKoRlauNFBPxFDaiGxxOGkzzh5wodyOODm4nC2EKzBakSqeBqQ23S-25snXwI1iwb5glWUPj-wjrIqBzAXLVGElFQ6AMz70noR75maP8wHJDE04XK_4tYjBuN4IyF2zGRvy2Gg18aCmjNTze0n61nF2KRjgrU65afrET_l6i4s5hMF2C7ClNy2F1JmUpm_BgKBB7t5DgaiY9QxhScim_uKLfp-5C4Z33D9aLuIDNlB2lEnKyTGlpHK5ipfnXIlcgo_MXRZFSXFK4I9PZLW_XNcE12Z2CX6aLbYz5anbAcPu7ioV1BfLqul2T-u1N1RxrSThYt4Aitn6HGb1HQpguzJfUKd9zf62VvuFKPCj-u9aUKNcAK42YwHVHLUlmhARHvkgnXuDIW074hg63ikt43UvV-VfLmTBpKrW2Nd4JmtIuxHtGAu-9JQTCADiF4hbHWFqTjtyPtypAy96nioCEcFzKDnoFlPEjHK2AdKT3V3fA72suEPsZGAadiw3uDz5XGcBESfLQ2wYuu4igEW2KU9JzbFlI_KobHeLpXTyYdOQXnn87qIiy4JFHZ8GtO-JKN8IFIuF6fV1LX7ESXvyjoFdKXTnpalM3tczNSDO4UetVwt2S3aK4xKrgharFYBTNRuyoaDWpU4J8Kz6bY05u7KL8fCWe7psrSZdo_2Jl9PfVWflfOdFJY0nrDe8Jwv0kR0-0IO6EOqwW1mlptkPrEXmoqD6FBvOUPe76PDYNu1mng; CrmOwinAuthC2=sNe6zdXrdeazo9IIKlKinj6wZqzpmOEO52D37CGKbszqkHKoYmsPGHhZDoeDrfiiWLs1NanDSaXKwruBMcPCyOI49Px4eh5yOAqHbx7yApDFPVQgqgqzb40Lc2p2nO08ah8ERF0b7IJEVGCFioK5E1iHqidLjyKGiZJmVEJZnZ0NYLNm05wiwG50WBeCQczwPLELL7RW1OfELQRQyj2UHdQMv0-dJxp8JqlhYow0_YBdJ33I5P4tdyRaqpCQ9dQEXqRs1VmtZgA-UOa6ONtR4E0IhhGZt0bP4YEuKoBx7Fa82XMXsMsEzkZC22xkyImpleoHo5IRucmNlq7v2yY03ZuXvdjtigoVpb2DT6XLnzCZs3kS9CX5ChyzM42BxtEeqofc9MWMXu7ExHiJ6H2xBCIYCa4i7EPDsecrb0ijfzTbHlUIxKZ-le6SQO47jYmecVsbIFOx4NkY9_ahj9gkPE3NBT064PmB43Qxm25gsyrW62u_jsTrtObSeBDM2MkbSURtRPs-IOKTBuOfKh_M42J5XxnTN9PYwXAcBg-mCPaozx07tGSLLvTb0d6P5TiOEnrQMS9YIZTv3BsY8LtSDwmZZWorg5Gxdc6gpi2GlkMi2PKVX0LlcD-VE3chbnzA22U7GHI21Iz7RxkcL-MAtlqf--TgTzSbCKmDjEmFUeSTXn54yXAZ2BER4jrae2Is8LpgXFW0euMTwF0sjnOryAT8kmDvM-82Kw8-yfhOI-0yeJwO17o96Ga8ODzRF-zuL3q8ELOmzJ_w88Q6ZOFsBcbSpe42LVOGgG6VLwIW0ceilmJ85YBr1dqJZWWUwDuVhbYGxJBZx3SUY36cLakr7k8xO97Gj64GSlA87609fLXBO9maDzTL0223hD7S1V3v_qPn7TSdDlpchs_jP29frbZvI2SZlUJ0GAUaemV_SgSLoxR2Yp-c_XlKQXuJ21zRxw8agYdMYYolmGeZoss13skhIdSkaZ79JoYPavI6XD6lEjaAks75BRfzSi81LgtE3DVQXt00ScDZmEhbCbLyBYimcwRiTQDQAnMUw5DdhAi250aGfJdEKZSeUvSQ-7-Qoz70kXrXZ0T8q2fsPnWbLbQarh-dG_ayJAv_2euiX_7HwXRpUcsnZeUghS6Ioie-TNocM3F8gXon-rJOiLPfyohnMzSedZuvf8Jl_tT6if32OE8_HMH4OcLL7HOe-pfFv6e7zgbz-uOPl-RFZX63d4_d6gbd7_S91Nn9UKEgAAA; ARRAffinity=d7f9fe12b844ba06d542963b23f8444a3f636363bb076be6a60aff3df60340db5631b1943ab55286fc737f03e6999c999b22afcfe8231c3bb3ed92fc4ce4384c08DE4B754AFA53C60000000485138620; ai_session=zJ2P7WbkOMre5bLIcZwA/v|1767511059917|1767518315089
            // mscrm.suppressduplicatedetection
            // false
            // origin
            // https://goto.crm4.dynamics.com
            // prefer
            // odata.include-annotations="*"
            // priority
            // u=1, i
            // referer
            // https://goto.crm4.dynamics.com/main.aspx?appid=0f6cc0bf-abf5-e811-a953-000d3a464508&newWindow=true&pagetype=entityrecord&etn=incident
            // sec-ch-ua
            // "Google Chrome";v="143", "Chromium";v="143", "Not A(Brand";v="24"
            // sec-ch-ua-mobile
            // ?0
            // sec-ch-ua-platform
            // "Windows"
            // sec-fetch-dest
            // empty
            // sec-fetch-mode
            // cors
            // sec-fetch-site
            // same-origin
            // user-agent
            // Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/143.0.0.0 Safari/537.36
            // x-ms-app-id
            // 0f6cc0bf-abf5-e811-a953-000d3a464508
            // x-ms-app-name
            // Customerservicehub
            // x-ms-client-request-id
            // 1fe92302-f5c2-4246-aeba-05e89bf332af
            // x-ms-client-session-id
            // 42a98e26-4d73-461b-a4ed-302e4255acbb
            // x-ms-correlation-id
            // 8e4aed70-8c09-41d8-a5f4-b801e24b3b60
            // x-ms-sw-objectid
            // b463b0ad-ff4f-4dca-8309-538adb5f5f7d
            // x-ms-sw-tenantid
            // d2256e74-8ec1-47b1-ae18-873765a27e8b
            // x-ms-user-agent
            // PowerApps-UCI/1.4.11287-2511.3 (Browser; AppName=Customerservicehub)
            // {
            //     "c2g_executiondate": "2025-10-28T06:35:00.000Z",
            //     "c2g_reportaddress": "הצורף 2",
            //     "new_municipality": "חולון",
            //     "c2g_ReportReason@odata.bind": "/c2g_reportreasons(a2a94c7c-1cab-ed11-aad0-6045bd8c985b)",
            //     "c2g_ReportReason@OData.Community.Display.V1.FormattedValue": "העמדת/ החנית את רכבך הנ\"ל לצד רכב אחר שעמד לצד הדרך (חניה כפולה)(א)",
            //     "gtg_ReportCity@odata.bind": "/gtg_cities(4ce8fb17-f8aa-ed11-aad0-6045bd895af9)",
            //     "gtg_ReportCity@OData.Community.Display.V1.FormattedValue": "אחר",
            //     "c2g_reportnumber": "14045470",
            //     "new_Vehicle@odata.bind": "/new_vehicles(9855b1c0-9b67-f011-bec2-6045bd958451)",
            //     "new_Vehicle@OData.Community.Display.V1.FormattedValue": "248-35-304",
            //     "new_Subject@odata.bind": "/new_subjects(c31f0689-8d47-ec11-8c61-6045bd8d2804)",
            //     "new_Subject@OData.Community.Display.V1.FormattedValue": "Fines & Authorities",
            //     "subjectid@odata.bind": "/subjects(c535548d-4b36-ea11-a813-000d3a27b751)",
            //     "subjectid@OData.Community.Display.V1.FormattedValue": "Parking Report",
            //     "new_SubSubject@odata.bind": "/new_subjects(30200689-8d47-ec11-8c61-6045bd8d2804)",
            //     "new_SubSubject@OData.Community.Display.V1.FormattedValue": "Parking fine",
            //     "c2g_repaircost3rdpartyvehicle": 0,
            //     "c2g_repaircostgotovehicle": 0,
            //     "c2g_costexpense": 0,
            //     "c2g_amountwerefundedourcustomer": 0,
            //     "c2g_amountwechargedourcustomer": 0,
            //     "c2g_amountwereceivedfrom3rdpartyourinsurance": 0,
            //     "title": "*Choose Subject*",
            //     "resolvebyslastatus": 1,
            //     "firstresponseslastatus": 1,
            //     "blockedprofile": false,
            //     "gtg_promotionemail": 962940000,
            //     "gtg_reminder": 962940001,
            //     "routecase": false,
            //     "gtg_isownerrelatedtocallcenter": false,
            //     "gtg_confirmation": 1,
            //     "new_rateplanchange": false,
            //     "new_licensecheckandupdatelicensetypes": false,
            //     "new_pincodesetting": false,
            //     "gtg_operationcasestatus": 962940000,
            //     "gtg_movetodamage": false,
            //     "gtg_leasingreport": true,
            //     "c2g_customerawaitingreply": false,
            //     "c2g_smartcardactivation": false,
            //     "c2g_reservationchangedtoiregular": false,
            //     "c2g_listeningtoacall": false,
            //     "c2g_taxiwasordered": false,
            //     "gtg_ispolicereport": false,
            //     "gtg_ticketonmaintenance": false,
            //     "c2g_towing": false,
            //     "c2g_accidentreportdocument": false,
            //     "c2g_accidentplacephoto": false,
            //     "c2g_damagephotosofthevehiclesinvolved": false,
            //     "c2g_3rdpartydriverslicensephoto": false,
            //     "c2g_3rdpartypolicyphoto": false,
            //     "gtg_canreturntoservicecode": 600920000,
            //     "c2g_rescue": false,
            //     "caseorigincode": 1,
            //     "gtg_priority": 962940003,
            //     "transactioncurrencyid@odata.bind": "/transactioncurrencies(efcf89c9-e053-ea11-a812-000d3a2d5883)",
            //     "transactioncurrencyid@OData.Community.Display.V1.FormattedValue": "שקל חדש",
            //     "statuscode": 1,
            //     "statecode": 0,
            //     "ownerid@odata.bind": "/systemusers(3cace5de-1be2-ef11-a731-0022488814a3)",
            //     "ownerid@OData.Community.Display.V1.FormattedValue": "ישראל ב.",
            //     "processid": "00000000-0000-0000-0000-000000000000",
            //     "new_CustomerNew@odata.bind": "/accounts(4540c22f-403a-f011-b4cc-0022488364f9)",
            //     "new_CustomerNew@OData.Community.Display.V1.FormattedValue": "כל-בו לחיות בית בעמ",
            //     "c2g_ResponsibleCustomer@odata.bind": "/accounts(4540c22f-403a-f011-b4cc-0022488364f9)",
            //     "c2g_ResponsibleCustomer@OData.Community.Display.V1.FormattedValue": "כל-בו לחיות בית בעמ",
            //     "c2g_BusinessUnit@odata.bind": "/businessunits(eb8212a1-c820-ea11-a810-000d3a2d5883)",
            //     "c2g_BusinessUnit@OData.Community.Display.V1.FormattedValue": "Israel",
            //     "customerid_account@odata.bind": "/accounts(4540c22f-403a-f011-b4cc-0022488364f9)",
            //     "customerid_account@OData.Community.Display.V1.FormattedValue": "כל-בו לחיות בית בעמ",
            //     "c2g_customer_search_account@odata.bind": "/accounts(4540c22f-403a-f011-b4cc-0022488364f9)",
            //     "c2g_customer_search_account@OData.Community.Display.V1.FormattedValue": "כל-בו לחיות בית בעמ"
            // }

            var downloadsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads"
            );

            // File paths
            var docxPath = Path.Combine(downloadsPath, $"Agreement - {name}.docx");

            // Extract embedded template
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"Reports.{toggle}_agreement.docx";

            using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    MessageBox.Show("Template not found in resources. Check resource name.");
                    return;
                }

                // Copy embedded template to a temp file so DocX can load it
                var tempTemplatePath = Path.Combine(Path.GetTempPath(), "template.docx");

                using (var fileStream = File.Create(tempTemplatePath))
                {
                    stream.CopyTo(fileStream);
                }

                // Load with DocX
                var doc = DocX.Load(tempTemplatePath);

                // Replace placeholders
                doc.ReplaceText(new StringReplaceTextOptions
                    {
                        SearchValue = "<<Name>>",
                        NewValue = name
                    }
                );
                doc.ReplaceText(new StringReplaceTextOptions{
                        SearchValue = "<<Date>>", 
                        NewValue = date
                    }
                );

                try
                {
                    // Save updated Word file
                    doc.SaveAs(docxPath);
                }
                catch (IOException)
                {
                    MessageBox.Show("Please close the document and try again.",
                        "File In Use",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = docxPath,
                UseShellExecute = true
            });

            Name.Text = "";
            Date.Text = "";
        }
    }
}
