// <copyright file="PresentWeather.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

/// <summary>
/// Represents the present weather component of the ATIS format.
/// </summary>
public class PresentWeather : BaseFormat
{
    private static readonly Dictionary<string, string> s_defaultWeatherDescriptors = new()
    {
        { "+DS", "Heavy Duststorm" },
        { "+DZ", "Heavy Drizzle" },
        { "+DZPL", "Heavy Drizzle And Ice Pellets" },
        { "+DZPLRA", "Heavy Drizzle, Ice Pellets And Rain" },
        { "+DZRA", "Heavy Drizzle And Rain" },
        { "+DZRAPL", "Heavy Drizzle, Rain And Ice Pellets" },
        { "+DZRASG", "Heavy Drizzle, Rain And Snow Grains" },
        { "+DZRASN", "Heavy Drizzle, Rain And Snow" },
        { "+DZSG", "Heavy Drizzle And Snow Grains" },
        { "+DZSGRA", "Heavy Drizzle, Snow Grains And Rain" },
        { "+DZSN", "Heavy Drizzle And Snow" },
        { "+DZSNRA", "Heavy Drizzle, Snow And Rain" },
        { "+FC", "Well-Developed Funnel Cloud" },
        { "+FZDZ", "Heavy Freezing Drizzle" },
        { "+FZDZRA", "Heavy Freezing Drizzle And Rain" },
        { "+FZRA", "Heavy Freezing Rain" },
        { "+FZRADZ", "Heavy Freezing Rain And Drizzle" },
        { "+FZUP", "Heavy Unidentified Freezing Precipitation" },
        { "+FZRASN", "Heavy freezing rain and snow" },
        { "+FZRAPL", "Heavy freezing rain and ice pellets" },
        { "+FZRASG", "Heavy freezing rain and snow grains" },
        { "+FZDZSG", "Heavy freezing drizzle and snow grains" },
        { "+FZDZSN", "Heavy freezing drizzle and snow" },
        { "+FZDZPL", "Heavy freezing drizzle and ice pellets" },
        { "+PL", "Heavy Ice Pellets" },
        { "+PLDZ", "Heavy Ice Pellets And Drizzle" },
        { "+PLDZRA", "Heavy Ice Pellets, Drizzle And Rain" },
        { "+PLRA", "Heavy Ice Pellets And Rain" },
        { "+PLRADZ", "Heavy Ice Pellets, Rain And Drizzle" },
        { "+PLRASN", "Heavy Ice Pellets, Rain And Snow" },
        { "+PLSG", "Heavy Ice Pellets And Snow Grains" },
        { "+PLSGSN", "Heavy Ice Pellets, Snow Grains And Snow" },
        { "+PLSN", "Heavy Ice Pellets And Snow" },
        { "+PLSNRA", "Heavy Ice Pellets, Snow And Rain" },
        { "+PLSNSG", "Heavy Ice Pellets, Snow And Snow Grains" },
        { "+RA", "Heavy Rain" },
        { "+RADZ", "Heavy Rain And Drizzle" },
        { "+RADZPL", "Heavy Rain, Drizzle And Ice Pellets" },
        { "+RADZSG", "Heavy Rain, Drizzle And Snow Grains" },
        { "+RADZSN", "Heavy Rain, Drizzle And Snow" },
        { "+RAPL", "Heavy Rain And Ice Pellets" },
        { "+RAPLDZ", "Heavy Rain, Ice Pellets And Drizzle" },
        { "+RAPLSN", "Heavy Rain, Ice Pellets And Snow" },
        { "+RASG", "Heavy Rain And Snow Grains" },
        { "+RASGDZ", "Heavy Rain, Snow Grains And Drizzle" },
        { "+RASGSN", "Heavy Rain, Snow Grains And Snow" },
        { "+RASN", "Heavy Rain And Snow" },
        { "+RASNDZ", "Heavy Rain, Snow And Drizzle" },
        { "+RASNPL", "Heavy Rain, Snow And Ice Pellets" },
        { "+RASNSG", "Heavy Rain, Snow And Snow Grains" },
        { "+SG", "Heavy Snow Grains" },
        { "+SGDZ", "Heavy Snow Grains And Drizzle" },
        { "+SGDZRA", "Heavy Snow Grains, Drizzle And Rain" },
        { "+SGPL", "Heavy Snow Grains And Ice Pellets" },
        { "+SGPLSN", "Heavy Snow Grains, Ice Pellets And Snow" },
        { "+SGRA", "Heavy Snow Grains And Rain" },
        { "+SGRADZ", "Heavy Snow Grains, Rain And Drizzle" },
        { "+SGRASN", "Heavy Snow Grains, Rain And Snow" },
        { "+SGSN", "Heavy Snow Grains And Snow" },
        { "+SGSNPL", "Heavy Snow Grains, Snow And Ice Pellets" },
        { "+SGSNRA", "Heavy Snow Grains, Snow And Rain" },
        { "+SHGR", "Heavy Showers Of Hail" },
        { "+SHGRRA", "Heavy Showers Of Hail And Rain" },
        { "+SHGRRASN", "Heavy Showers Of Hail, Rain And Snow" },
        { "+SHGRSN", "Heavy Showers Of Hail And Snow" },
        { "+SHGRSNRA", "Heavy Showers Of Hail, Snow And Rain" },
        { "+SHGS", "Heavy Showers Of Snow Pellets" },
        { "+SHGSRA", "Heavy Showers Of Snow Pellets And Rain" },
        { "+SHGSRASN", "Heavy Showers Of Snow Pellets, Rain And Snow" },
        { "+SHGSSN", "Heavy Showers Of Snow Pellets And Snow" },
        { "+SHGSSNRA", "Heavy Showers Of Snow Pellets, Snow And Rain" },
        { "+SHRA", "Heavy Showers Of Rain" },
        { "+SHRAGR", "Heavy Showers Of Rain And Hail" },
        { "+SHRAGRSN", "Heavy Showers Of Rain, Hail And Snow" },
        { "+SHRAGS", "Heavy Showers Of Rain And Snow Pellets" },
        { "+SHRAGSSN", "Heavy Showers Of Rain, Snow Pellets And Snow" },
        { "+SHRASN", "Heavy Showers Of Rain And Snow" },
        { "+SHRASNGR", "Heavy Showers Of Rain, Snow And Hail" },
        { "+SHRASNGS", "Heavy Showers Of Rain, Snow And Snow Pellets" },
        { "+SHSN", "Heavy Showers Of Snow" },
        { "+SHSNGR", "Heavy Showers Of Snow And Hail" },
        { "+SHSNGRRA", "Heavy Showers Of Snow, Hail And Rain" },
        { "+SHSNGS", "Heavy Showers Of Snow And Snow Pellets" },
        { "+SHSNGSRA", "Heavy Showers Of Snow, Snow Pellets And Rain" },
        { "+SHSNRA", "Heavy Showers Of Snow And Rain" },
        { "+SHSNRAGR", "Heavy Showers Of Snow, Rain And Hail" },
        { "+SHSNRAGS", "Heavy Showers Of Snow, Rain And Snow Pellets" },
        { "+SHUP", "Heavy Unidentified Showers Of Precipitation" },
        { "+SN", "Heavy Snow" },
        { "+SNDZ", "Heavy Snow And Drizzle" },
        { "+SNDZRA", "Heavy Snow, Drizzle And Rain" },
        { "+SNPL", "Heavy Snow And Ice Pellets" },
        { "+SNPLRA", "Heavy Snow, Ice Pellets And Rain" },
        { "+SNPLSG", "Heavy Snow, Ice Pellets And Snow Grains" },
        { "+SNRA", "Heavy Snow And Rain" },
        { "+SNRADZ", "Heavy Snow, Rain And Drizzle" },
        { "+SNRAPL", "Heavy Snow, Rain And Ice Pellets" },
        { "+SNRASG", "Heavy Snow, Rain And Snow Grains" },
        { "+SNSG", "Heavy Snow And Snow Grains" },
        { "+SNSGPL", "Heavy Snow, Snow Grains And Ice Pellets" },
        { "+SNSGRA", "Heavy Snow, Snow Grains And Rain" },
        { "+SS", "Heavy Sandstorm" },
        { "+TSGR", "Thunderstorm With Heavy Hail" },
        { "+TSGRRA", "Thunderstorm With Heavy Hail And Rain" },
        { "+TSGRRASN", "Thunderstorm With Heavy Hail, Rain And Snow" },
        { "+TSGRSN", "Thunderstorm With Heavy Hail And Snow" },
        { "+TSGRSNRA", "Thunderstorm With Heavy Hail, Snow And Rain" },
        { "+TSGS", "Thunderstorm With Heavy Snow Pellets" },
        { "+TSGSRA", "Thunderstorm With Heavy Snow Pellets And Rain" },
        { "+TSGSRASN", "Thunderstorm With Heavy Snow Pellets, Rain And Snow" },
        { "+TSGSSN", "Thunderstorm With Heavy Snow Pellets And Snow" },
        { "+TSGSSNRA", "Thunderstorm With Heavy Snow Pellets, Snow And Rain" },
        { "+TSRA", "Thunderstorm With Heavy Rain" },
        { "+TSRAGR", "Thunderstorm With Heavy Rain And Hail" },
        { "+TSRAGRSN", "Thunderstorm With Heavy Rain, Hail And Snow" },
        { "+TSRAGS", "Thunderstorm With Heavy Rain And Snow Pellets" },
        { "+TSRAGSSN", "Thunderstorm With Heavy Rain, Snow Pellets And Snow" },
        { "+TSRASN", "Thunderstorm With Heavy Rain And Snow" },
        { "+TSRASNGR", "Thunderstorm With Heavy Rain, Snow And Hail" },
        { "+TSRASNGS", "Thunderstorm With Heavy Rain, Snow And Snow Pellets" },
        { "+TSSN", "Thunderstorm With Heavy Snow" },
        { "+TSSNGR", "Thunderstorm With Heavy Snow And Hail" },
        { "+TSSNGRRA", "Thunderstorm With Heavy Snow, Hail And Rain" },
        { "+TSSNGS", "Thunderstorm With Heavy Snow And Snow Pellets" },
        { "+TSSNGSRA", "Thunderstorm With Heavy Snow, Snow Pellets And Rain" },
        { "+TSSNRA", "Thunderstorm With Heavy Snow And Rain" },
        { "+TSSNRAGR", "Thunderstorm With Heavy Snow, Rain And Hail" },
        { "+TSSNRAGS", "Thunderstorm With Heavy Snow, Rain And Snow Pellets" },
        { "+TSUP", "Thunderstorm With Heavy Unidentified Precipitation" },
        { "+UP", "Heavy Unidentified Precipitation" },
        { "-DS", "Light Duststorm" },
        { "-DZ", "Light Drizzle" },
        { "-DZPL", "Light Drizzle And Ice Pellets" },
        { "-DZPLRA", "Light Drizzle, Ice Pellets And Rain" },
        { "-DZRA", "Light Drizzle And Rain" },
        { "-DZRAPL", "Light Drizzle, Rain And Ice Pellets" },
        { "-DZRASG", "Light Drizzle, Rain And Snow Grains" },
        { "-DZRASN", "Light Drizzle, Rain And Snow" },
        { "-DZSG", "Light Drizzle And Snow Grains" },
        { "-DZSGRA", "Light Drizzle, Snow Grains And Rain" },
        { "-DZSN", "Light Drizzle And Snow" },
        { "-DZSNRA", "Light Drizzle, Snow And Rain" },
        { "-FZDZ", "Light Freezing Drizzle" },
        { "-FZDZRA", "Light Freezing Drizzle And Rain" },
        { "-FZRA", "Light Freezing Rain" },
        { "-FZRADZ", "Light Freezing Rain And Drizzle" },
        { "-FZUP", "Light Unidentified Freezing Precipitation" },
        { "-FZRASN", "Light freezing rain and snow" },
        { "-FZRAPL", "Light freezing rain and ice pellets" },
        { "-FZRASG", "Light freezing rain and snow grains" },
        { "-FZDZSG", "Light freezing drizzle and snow grains" },
        { "-FZDZSN", "Light freezing drizzle and snow" },
        { "-FZDZPL", "Light freezing drizzle and ice pellets" },
        { "-PL", "Light Ice Pellets" },
        { "-PLDZ", "Light Ice Pellets And Drizzle" },
        { "-PLDZRA", "Light Ice Pellets, Drizzle And Rain" },
        { "-PLRA", "Light Ice Pellets And Rain" },
        { "-PLRADZ", "Light Ice Pellets, Rain And Drizzle" },
        { "-PLRASN", "Light Ice Pellets, Rain And Snow" },
        { "-PLSG", "Light Ice Pellets And Snow Grains" },
        { "-PLSGSN", "Light Ice Pellets, Snow Grains And Snow" },
        { "-PLSN", "Light Ice Pellets And Snow" },
        { "-PLSNRA", "Light Ice Pellets, Snow And Rain" },
        { "-PLSNSG", "Light Ice Pellets, Snow And Snow Grains" },
        { "-RA", "Light Rain" },
        { "-RADZ", "Light Rain And Drizzle" },
        { "-RADZPL", "Light Rain, Drizzle And Ice Pellets" },
        { "-RADZSG", "Light Rain, Drizzle And Snow Grains" },
        { "-RADZSN", "Light Rain, Drizzle And Snow" },
        { "-RAPL", "Light Rain And Ice Pellets" },
        { "-RAPLDZ", "Light Rain, Ice Pellets And Drizzle" },
        { "-RAPLSN", "Light Rain, Ice Pellets And Snow" },
        { "-RASG", "Light Rain And Snow Grains" },
        { "-RASGDZ", "Light Rain, Snow Grains And Drizzle" },
        { "-RASGSN", "Light Rain, Snow Grains And Snow" },
        { "-RASN", "Light Rain And Snow" },
        { "-RASNDZ", "Light Rain, Snow And Drizzle" },
        { "-RASNPL", "Light Rain, Snow And Ice Pellets" },
        { "-RASNSG", "Light Rain, Snow And Snow Grains" },
        { "-SG", "Light Snow Grains" },
        { "-SGDZ", "Light Snow Grains And Drizzle" },
        { "-SGDZRA", "Light Snow Grains, Drizzle And Rain" },
        { "-SGPL", "Light Snow Grains And Ice Pellets" },
        { "-SGPLSN", "Light Snow Grains, Ice Pellets And Snow" },
        { "-SGRA", "Light Snow Grains And Rain" },
        { "-SGRADZ", "Light Snow Grains, Rain And Drizzle" },
        { "-SGRASN", "Light Snow Grains, Rain And Snow" },
        { "-SGSN", "Light Snow Grains And Snow" },
        { "-SGSNPL", "Light Snow Grains, Snow And Ice Pellets" },
        { "-SGSNRA", "Light Snow Grains, Snow And Rain" },
        { "-SHGR", "Light Showers Of Hail" },
        { "-SHGRRA", "Light Showers Of Hail And Rain" },
        { "-SHGRRASN", "Light Showers Of Hail, Rain And Snow" },
        { "-SHGRSN", "Light Showers Of Hail And Snow" },
        { "-SHGRSNRA", "Light Showers Of Hail, Snow And Rain" },
        { "-SHGS", "Light Showers Of Snow Pellets" },
        { "-SHGSRA", "Light Showers Of Snow Pellets And Rain" },
        { "-SHGSRASN", "Light Showers Of Snow Pellets, Rain And Snow" },
        { "-SHGSSN", "Light Showers Of Snow Pellets And Snow" },
        { "-SHGSSNRA", "Light Showers Of Snow Pellets, Snow And Rain" },
        { "-SHRA", "Light Showers Of Rain" },
        { "-SHRAGR", "Light Showers Of Rain And Hail" },
        { "-SHRAGRSN", "Light Showers Of Rain, Hail And Snow" },
        { "-SHRAGS", "Light Showers Of Rain And Snow Pellets" },
        { "-SHRAGSSN", "Light Showers Of Rain, Snow Pellets And Snow" },
        { "-SHRASN", "Light Showers Of Rain And Snow" },
        { "-SHRASNGR", "Light Showers Of Rain, Snow And Hail" },
        { "-SHRASNGS", "Light Showers Of Rain, Snow And Snow Pellets" },
        { "-SHSN", "Light Showers Of Snow" },
        { "-SHSNGR", "Light Showers Of Snow And Hail" },
        { "-SHSNGRRA", "Light Showers Of Snow, Hail And Rain" },
        { "-SHSNGS", "Light Showers Of Snow And Snow Pellets" },
        { "-SHSNGSRA", "Light Showers Of Snow, Snow Pellets And Rain" },
        { "-SHSNRA", "Light Showers Of Snow And Rain" },
        { "-SHSNRAGR", "Light Showers Of Snow, Rain And Hail" },
        { "-SHSNRAGS", "Light Showers Of Snow, Rain And Snow Pellets" },
        { "-SHUP", "Light Unidentified Showers Of Precipitation" },
        { "-SN", "Light Snow" },
        { "-SNDZ", "Light Snow And Drizzle" },
        { "-SNDZRA", "Light Snow, Drizzle And Rain" },
        { "-SNPL", "Light Snow And Ice Pellets" },
        { "-SNPLRA", "Light Snow, Ice Pellets And Rain" },
        { "-SNPLSG", "Light Snow, Ice Pellets And Snow Grains" },
        { "-SNRA", "Light Snow And Rain" },
        { "-SNRADZ", "Light Snow, Rain And Drizzle" },
        { "-SNRAPL", "Light Snow, Rain And Ice Pellets" },
        { "-SNRASG", "Light Snow, Rain And Snow Grains" },
        { "-SNSG", "Light Snow And Snow Grains" },
        { "-SNSGPL", "Light Snow, Snow Grains And Ice Pellets" },
        { "-SNSGRA", "Light Snow, Snow Grains And Rain" },
        { "-SS", "Light Sandstorm" },
        { "-TSGR", "Thunderstorm With Light Hail" },
        { "-TSGRRA", "Thunderstorm With Light Hail And Rain" },
        { "-TSGRRASN", "Thunderstorm With Light Hail, Rain And Snow" },
        { "-TSGRSN", "Thunderstorm With Light Hail And Snow" },
        { "-TSGRSNRA", "Thunderstorm With Light Hail, Snow And Rain" },
        { "-TSGS", "Thunderstorm With Light Snow Pellets" },
        { "-TSGSRA", "Thunderstorm With Light Snow Pellets And Rain" },
        { "-TSGSRASN", "Thunderstorm With Light Snow Pellets, Rain And Snow" },
        { "-TSGSSN", "Thunderstorm With Light Snow Pellets And Snow" },
        { "-TSGSSNRA", "Thunderstorm With Light Snow Pellets, Snow And Rain" },
        { "-TSRA", "Thunderstorm With Light Rain" },
        { "-TSRAGR", "Thunderstorm With Light Rain And Hail" },
        { "-TSRAGRSN", "Thunderstorm With Light Rain, Hail And Snow" },
        { "-TSRAGS", "Thunderstorm With Light Rain And Snow Pellets" },
        { "-TSRAGSSN", "Thunderstorm With Light Rain, Snow Pellets And Snow" },
        { "-TSRASN", "Thunderstorm With Light Rain And Snow" },
        { "-TSRASNGR", "Thunderstorm With Light Rain, Snow And Hail" },
        { "-TSRASNGS", "Thunderstorm With Light Rain, Snow And Snow Pellets" },
        { "-TSSN", "Thunderstorm With Light Snow" },
        { "-TSSNGR", "Thunderstorm With Light Snow And Hail" },
        { "-TSSNGRRA", "Thunderstorm With Light Snow, Hail And Rain" },
        { "-TSSNGS", "Thunderstorm With Light Snow And Snow Pellets" },
        { "-TSSNGSRA", "Thunderstorm With Light Snow, Snow Pellets And Rain" },
        { "-TSSNRA", "Thunderstorm With Light Snow And Rain" },
        { "-TSSNRAGR", "Thunderstorm With Light Snow, Rain And Hail" },
        { "-TSSNRAGS", "Thunderstorm With Light Snow, Rain And Snow Pellets" },
        { "-TSUP", "Thunderstorm With Light Unidentified Precipitation" },
        { "-UP", "Light Unidentified Precipitation" },
        { "BCFG", "Patches Of Fog" },
        { "BLDU", "Blowing Dust" },
        { "BLSA", "Blowing Sand" },
        { "BLSN", "Blowing Snow" },
        { "BR", "Mist" },
        { "DRDU", "Low Drifting Dust" },
        { "DRSA", "Low Drifting Sand" },
        { "DRSN", "Low Drifting Snow" },
        { "DS", "Duststorm" },
        { "DU", "Dust" },
        { "DZ", "Drizzle" },
        { "DZPL", "Drizzle And Ice Pellets" },
        { "DZPLRA", "Drizzle, Ice Pellets And Rain" },
        { "DZRA", "Drizzle And Rain" },
        { "DZRAPL", "Drizzle, Rain And Ice Pellets" },
        { "DZRASG", "Drizzle, Rain And Snow Grains" },
        { "DZRASN", "Drizzle, Rain And Snow" },
        { "DZSG", "Drizzle And Snow Grains" },
        { "DZSGRA", "Drizzle, Snow Grains And Rain" },
        { "DZSN", "Drizzle And Snow" },
        { "DZSNRA", "Drizzle, Snow And Rain" },
        { "FC", "Funnel Cloud" },
        { "FG", "Fog" },
        { "FU", "Smoke" },
        { "FZDZ", "Freezing Drizzle" },
        { "FZDZRA", "Freezing Drizzle And Rain" },
        { "FZFG", "Freezing Fog" },
        { "FZRA", "Freezing Rain" },
        { "FZRADZ", "Freezing Rain And Drizzle" },
        { "FZUP", "Unidentified Freezing Precipitation" },
        { "FZRASN", "Freezing rain and snow" },
        { "FZRAPL", "Freezing rain and ice pellets" },
        { "FZRASG", "Freezing rain and snow grains" },
        { "FZDZSG", "Freezing drizzle and snow grains" },
        { "FZDZSN", "Freezing drizzle and snow" },
        { "FZDZPL", "Freezing drizzle and ice pellets" },
        { "HZ", "Haze" },
        { "MIFG", "Shallow Fog" },
        { "PL", "Ice Pellets" },
        { "PLDZ", "Ice Pellets And Drizzle" },
        { "PLDZRA", "Ice Pellets, Drizzle And Rain" },
        { "PLRA", "Ice Pellets And Rain" },
        { "PLRADZ", "Ice Pellets, Rain And Drizzle" },
        { "PLRASN", "Ice Pellets, Rain And Snow" },
        { "PLSG", "Ice Pellets And Snow Grains" },
        { "PLSGSN", "Ice Pellets, Snow Grains And Snow" },
        { "PLSN", "Ice Pellets And Snow" },
        { "PLSNRA", "Ice Pellets, Snow And Rain" },
        { "PLSNSG", "Ice Pellets, Snow And Snow Grains" },
        { "PO", "Dust/Sand Whirls" },
        { "PRFG", "Partial Fog" },
        { "RA", "Rain" },
        { "RADZ", "Rain And Drizzle" },
        { "RADZPL", "Rain, Drizzle And Ice Pellets" },
        { "RADZSG", "Rain, Drizzle And Snow Grains" },
        { "RADZSN", "Rain, Drizzle And Snow" },
        { "RAPL", "Rain And Ice Pellets" },
        { "RAPLDZ", "Rain, Ice Pellets And Drizzle" },
        { "RAPLSN", "Rain, Ice Pellets And Snow" },
        { "RASG", "Rain And Snow Grains" },
        { "RASGDZ", "Rain, Snow Grains And Drizzle" },
        { "RASGSN", "Rain, Snow Grains And Snow" },
        { "RASN", "Rain And Snow" },
        { "RASNDZ", "Rain, Snow And Drizzle" },
        { "RASNPL", "Rain, Snow And Ice Pellets" },
        { "RASNSG", "Rain, Snow And Snow Grains" },
        { "SA", "Sand" },
        { "SG", "Snow Grains" },
        { "SGDZ", "Snow Grains And Drizzle" },
        { "SGDZRA", "Snow Grains, Drizzle And Rain" },
        { "SGPL", "Snow Grains And Ice Pellets" },
        { "SGPLSN", "Snow Grains, Ice Pellets And Snow" },
        { "SGRA", "Snow Grains And Rain" },
        { "SGRADZ", "Snow Grains, Rain And Drizzle" },
        { "SGRASN", "Snow Grains, Rain And Snow" },
        { "SGSN", "Snow Grains And Snow" },
        { "SGSNPL", "Snow Grains, Snow And Ice Pellets" },
        { "SGSNRA", "Snow Grains, Snow And Rain" },
        { "SHGR", "Showers Of Hail" },
        { "SHGRRA", "Showers Of Hail And Rain" },
        { "SHGRRASN", "Showers Of Hail, Rain And Snow" },
        { "SHGRSN", "Showers Of Hail And Snow" },
        { "SHGRSNRA", "Showers Of Hail, Snow And Rain" },
        { "SHGS", "Showers Of Snow Pellets" },
        { "SHGSRA", "Showers Of Snow Pellets And Rain" },
        { "SHGSRASN", "Showers Of Snow Pellets, Rain And Snow" },
        { "SHGSSN", "Showers Of Snow Pellets And Snow" },
        { "SHGSSNRA", "Showers Of Snow Pellets, Snow And Rain" },
        { "SHRA", "Showers Of Rain" },
        { "SHRAGR", "Showers Of Rain And Hail" },
        { "SHRAGRSN", "Showers Of Rain, Hail And Snow" },
        { "SHRAGS", "Showers Of Rain And Snow Pellets" },
        { "SHRAGSSN", "Showers Of Rain, Snow Pellets And Snow" },
        { "SHRASN", "Showers Of Rain And Snow" },
        { "SHRASNGR", "Showers Of Rain, Snow And Hail" },
        { "SHRASNGS", "Showers Of Rain, Snow And Snow Pellets" },
        { "SHSN", "Showers Of Snow" },
        { "SHSNGR", "Showers Of Snow And Hail" },
        { "SHSNGRRA", "Showers Of Snow, Hail And Rain" },
        { "SHSNGS", "Showers Of Snow And Snow Pellets" },
        { "SHSNGSRA", "Showers Of Snow, Snow Pellets And Rain" },
        { "SHSNRA", "Showers Of Snow And Rain" },
        { "SHSNRAGR", "Showers Of Snow, Rain And Hail" },
        { "SHSNRAGS", "Showers Of Snow, Rain And Snow Pellets" },
        { "SHUP", "Unidentified Showers Of Precipitation" },
        { "SN", "Snow" },
        { "SNDZ", "Snow And Drizzle" },
        { "SNDZRA", "Snow, Drizzle And Rain" },
        { "SNPL", "Snow And Ice Pellets" },
        { "SNPLRA", "Snow, Ice Pellets And Rain" },
        { "SNPLSG", "Snow, Ice Pellets And Snow Grains" },
        { "SNRA", "Snow And Rain" },
        { "SNRADZ", "Snow, Rain And Drizzle" },
        { "SNRAPL", "Snow, Rain And Ice Pellets" },
        { "SNRASG", "Snow, Rain And Snow Grains" },
        { "SNSG", "Snow And Snow Grains" },
        { "SNSGPL", "Snow, Snow Grains And Ice Pellets" },
        { "SNSGRA", "Snow, Snow Grains And Rain" },
        { "SQ", "Squalls" },
        { "SS", "Sandstorm" },
        { "TS", "Thunderstorm" },
        { "TSGR", "Thunderstorm With Hail" },
        { "TSGRRA", "Thunderstorm With Hail And Rain" },
        { "TSGRRASN", "Thunderstorm With Hail, Rain And Snow" },
        { "TSGRSN", "Thunderstorm With Hail And Snow" },
        { "TSGRSNRA", "Thunderstorm With Hail, Snow And Rain" },
        { "TSGS", "Thunderstorm With Snow Pellets" },
        { "TSGSRA", "Thunderstorm With Snow Pellets And Rain" },
        { "TSGSRASN", "Thunderstorm With Snow Pellets, Rain And Snow" },
        { "TSGSSN", "Thunderstorm With Snow Pellets And Snow" },
        { "TSGSSNRA", "Thunderstorm With Snow Pellets, Snow And Rain" },
        { "TSRA", "Thunderstorm With Rain" },
        { "TSRAGR", "Thunderstorm With Rain And Hail" },
        { "TSRAGRSN", "Thunderstorm With Rain, Hail And Snow" },
        { "TSRAGS", "Thunderstorm With Rain And Snow Pellets" },
        { "TSRAGSSN", "Thunderstorm With Rain, Snow Pellets And Snow" },
        { "TSRASN", "Thunderstorm With Rain And Snow" },
        { "TSRASNGR", "Thunderstorm With Rain, Snow And Hail" },
        { "TSRASNGS", "Thunderstorm With Rain, Snow And Snow Pellets" },
        { "TSSN", "Thunderstorm With Snow" },
        { "TSSNGR", "Thunderstorm With Snow And Hail" },
        { "TSSNGRRA", "Thunderstorm With Snow, Hail And Rain" },
        { "TSSNGS", "Thunderstorm With Snow And Snow Pellets" },
        { "TSSNGSRA", "Thunderstorm With Snow, Snow Pellets And Rain" },
        { "TSSNRA", "Thunderstorm With Snow And Rain" },
        { "TSSNRAGR", "Thunderstorm With Snow, Rain And Hail" },
        { "TSSNRAGS", "Thunderstorm With Snow, Rain And Snow Pellets" },
        { "TSUP", "Thunderstorm With Unidentified Precipitation" },
        { "UP", "Unidentified Precipitation" },
        { "VA", "Volcanic Ash" },
        { "VCBLDU", "Blowing Dust In The Vicinity" },
        { "VCBLSA", "Blowing Sand In The Vicinity" },
        { "VCBLSN", "Blowing Snow In The Vicinity" },
        { "VCDS", "Duststorm In The Vicinity" },
        { "VCFC", "Funnel Cloud In The Vicinity" },
        { "VCFG", "Fog In The Vicinity" },
        { "VCPO", "Dust/Sand Whirls In The Vicinity" },
        { "VCSH", "Showers In The Vicinity" },
        { "VCSS", "Sandstorm In The Vicinity" },
        { "VCTS", "Thunderstorm In The Vicinity" },
        { "VCVA", "Volcanic Ash In The Vicinity" },
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="PresentWeather"/> class.
    /// </summary>
    public PresentWeather()
    {
        EnsureDefaultWeatherTypes();

        Template = new Template { Text = "{weather}", Voice = "{weather}", };
    }

    /// <summary>
    /// Gets or sets the dictionary of present weather types.
    /// </summary>
    public Dictionary<string, WeatherDescriptorType> PresentWeatherTypes { get; set; } = new();

    /// <summary>
    /// Gets or sets the legacy weather types. This property is obsolete and should not be used.
    /// </summary>
    [Obsolete("Use 'PresentWeatherTypes' instead")]
    [JsonPropertyName("WeatherTypes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Dictionary<string, string>? LegacyWeatherTypes
    {
        get => null;
        set
        {
            if (value != null)
            {
                foreach (var kvp in value)
                {
                    PresentWeatherTypes[kvp.Key] = new WeatherDescriptorType(kvp.Key, kvp.Value);
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the legacy weather descriptors. This property is obsolete and should not be used.
    /// </summary>
    [Obsolete("Use 'PresentWeatherTypes' instead")]
    [JsonPropertyName("weatherDescriptors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Dictionary<string, string>? LegacyWeatherDescriptors
    {
        get => null;
        set
        {
            if (value != null)
            {
                foreach (var kvp in value)
                {
                    PresentWeatherTypes[kvp.Key] = new WeatherDescriptorType(kvp.Key, kvp.Value);
                }
            }
        }
    }

    /// <summary>
    /// Creates a new instance of <see cref="PresentWeather"/> that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="PresentWeather"/> instance that is a copy of this instance.</returns>
    public PresentWeather Clone()
    {
        return (PresentWeather)MemberwiseClone();
    }

    /// <summary>
    /// Ensures that the default weather types are present in the dictionary.
    /// </summary>
    public void EnsureDefaultWeatherTypes()
    {
        foreach (var kvp in s_defaultWeatherDescriptors)
        {
            if (!PresentWeatherTypes.ContainsKey(kvp.Key))
            {
                PresentWeatherTypes[kvp.Key] = new WeatherDescriptorType(kvp.Key, kvp.Value);
            }
        }

        // Sort the dictionary by keys after insertion
        PresentWeatherTypes = PresentWeatherTypes
            .OrderBy(kvp => kvp.Key) // Sort by the key alphabetically
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Represents a weather descriptor type with text and spoken components.
    /// </summary>
    public record WeatherDescriptorType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WeatherDescriptorType"/> class.
        /// </summary>
        /// <param name="text">The text component of the weather descriptor.</param>
        /// <param name="spoken">The spoken component of the weather descriptor.</param>
        public WeatherDescriptorType(string text, string spoken)
        {
            Text = text;
            Spoken = spoken;
        }

        /// <summary>
        /// Gets or sets the text component of the weather descriptor.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the spoken component of the weather descriptor.
        /// </summary>
        public string Spoken { get; set; }
    }
}
