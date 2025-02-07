﻿//This file is part of AudioCuesheetEditor.

//AudioCuesheetEditor is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//AudioCuesheetEditor is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with Foobar.  If not, see
//<http: //www.gnu.org/licenses />.
using AudioCuesheetEditor.Model.AudioCuesheet;
using AudioCuesheetEditor.Model.Entity;
using AudioCuesheetEditor.Model.IO.Audio;
using AudioCuesheetEditor.Model.Options;
using System.Data;
using System.Diagnostics.Metrics;
using System.Text;
using System.Xml.Linq;

namespace AudioCuesheetEditor.Model.IO.Export
{
    public enum ExportType
    {
        Cuesheet = 0,
        Exportprofile
    }

    public class ExportfileGenerator : Validateable<ExportfileGenerator>
    {
        public Cuesheet Cuesheet { get; }
        public Exportprofile? Exportprofile { get; set; }
        public ApplicationOptions? ApplicationOptions { get; set; }
        public ExportType ExportType { get; set; }

        public ExportfileGenerator(ExportType exportType, Cuesheet cuesheet, Exportprofile? exportprofile = null, ApplicationOptions? applicationOptions = null)
        {
            ExportType = exportType;
            Cuesheet = cuesheet;
            Exportprofile = exportprofile;
            ApplicationOptions = applicationOptions;
        }

        public IReadOnlyCollection<Exportfile> GenerateExportfiles()
        {
            List<Exportfile> exportfiles = new();
            if (Validate().Status != ValidationStatus.Error)
            {
                if (Cuesheet.SplitPoints.Count != 0)
                {
                    TimeSpan? previousSplitPointMoment = null;
                    var begin = Cuesheet.Tracks.Min(x => x.Begin);
                    var counter = 1;
                    String? content = null;
                    String filename = String.Empty;
                    String? audioFileName = null;
                    foreach (var splitPoint in Cuesheet.SplitPoints.OrderBy(x => x.Moment))
                    {
                        audioFileName = splitPoint.AudiofileName;
                        if (splitPoint.Validate().Status == ValidationStatus.Success)
                        {
                            switch (ExportType)
                            {
                                case ExportType.Cuesheet:
                                    content = WriteCuesheet(audioFileName, previousSplitPointMoment, splitPoint);
                                    filename = String.Format("{0}({1}){2}", Path.GetFileNameWithoutExtension(ApplicationOptions?.CuesheetFilename), counter, Cuesheet.FileExtension);
                                    break;
                                case ExportType.Exportprofile:
                                    if (Exportprofile != null)
                                    {
                                        content = WriteExport(audioFileName, previousSplitPointMoment, splitPoint);
                                        filename = String.Format("{0}({1}){2}", Path.GetFileNameWithoutExtension(Exportprofile.Filename), counter, Path.GetExtension(Exportprofile.Filename));
                                    }
                                    break;
                            }
                            if (content != null)
                            {
                                if (previousSplitPointMoment != null)
                                {
                                    begin = previousSplitPointMoment;
                                }
                                exportfiles.Add(new Exportfile() { Name = filename, Content = Encoding.UTF8.GetBytes(content), Begin = begin, End = splitPoint.Moment });
                            }
                            previousSplitPointMoment = splitPoint.Moment;
                            counter++;
                        }
                    }
                    //After a split point attach the last part
                    audioFileName = String.Format("{0}({1}){2}", Path.GetFileNameWithoutExtension(Cuesheet.Audiofile?.Name), counter, Path.GetExtension(Cuesheet.Audiofile?.Name));
                    switch (ExportType)
                    {
                        case ExportType.Cuesheet:
                            content = WriteCuesheet(audioFileName, previousSplitPointMoment);
                            filename = String.Format("{0}({1}){2}", Path.GetFileNameWithoutExtension(ApplicationOptions?.CuesheetFilename), counter, Cuesheet.FileExtension);
                            break;
                        case ExportType.Exportprofile:
                            if (Exportprofile != null)
                            {
                                content = WriteExport(audioFileName, previousSplitPointMoment);
                                filename = String.Format("{0}({1}){2}", Path.GetFileNameWithoutExtension(Exportprofile.Filename), counter, Path.GetExtension(Exportprofile.Filename));
                            }
                            break;
                    }
                    if (content != null)
                    {
                        var end = Cuesheet.Tracks.Max(x => x.End);
                        exportfiles.Add(new Exportfile() { Name = filename, Content = Encoding.UTF8.GetBytes(content), Begin = previousSplitPointMoment, End = end });
                    }
                }
                else
                {
                    String filename = String.Empty;
                    String? content = null;
                    switch (ExportType)
                    {
                        case ExportType.Cuesheet:
                            var cuesheetfilename = ApplicationOptions?.CuesheetFilename;
                            if (String.IsNullOrEmpty(cuesheetfilename) == false)
                            {
                                filename = cuesheetfilename;
                            }
                            else
                            {
                                filename = Exportfile.DefaultCuesheetFilename;
                            }
                            if (Cuesheet.Audiofile != null)
                            {
                                content = WriteCuesheet(Cuesheet.Audiofile.Name);
                            }
                            break;
                        case ExportType.Exportprofile:
                            if (Exportprofile != null)
                            {
                                filename = Exportprofile.Filename;
                                if (Cuesheet.Audiofile != null)
                                {
                                    content = WriteExport(Cuesheet.Audiofile.Name);
                                }
                            }
                            break;
                    }
                    if (content != null)
                    {
                        var begin = Cuesheet.Tracks.Min(x => x.Begin);
                        var end = Cuesheet.Tracks.Max(x => x.End);
                        exportfiles.Add(new Exportfile() { Name = filename, Content = Encoding.UTF8.GetBytes(content), Begin = begin, End = end });
                    }
                }
            }
            return exportfiles;
        }

        private string WriteCuesheet(String? audiofileName, TimeSpan? from = null, SplitPoint? splitPoint = null)
        {
            var builder = new StringBuilder();
            if (Cuesheet.Cataloguenumber != null && string.IsNullOrEmpty(Cuesheet.Cataloguenumber.Value) == false && Cuesheet.Cataloguenumber.Validate().Status != ValidationStatus.Error)
            {
                builder.AppendLine(string.Format("{0} {1}", CuesheetConstants.CuesheetCatalogueNumber, Cuesheet.Cataloguenumber.Value));
            }
            if (Cuesheet.CDTextfile != null)
            {
                builder.AppendLine(string.Format("{0} \"{1}\"", CuesheetConstants.CuesheetCDTextfile, Cuesheet.CDTextfile.Name));
            }
            builder.AppendLine(string.Format("{0} \"{1}\"", CuesheetConstants.CuesheetTitle, splitPoint != null ? splitPoint.Title : Cuesheet.Title));
            builder.AppendLine(string.Format("{0} \"{1}\"", CuesheetConstants.CuesheetArtist, splitPoint != null ? splitPoint.Artist : Cuesheet.Artist));
            builder.AppendLine(string.Format("{0} \"{1}\" {2}", CuesheetConstants.CuesheetFileName, audiofileName, Cuesheet.Audiofile?.AudioFileType));
            IEnumerable<Track> tracks = Cuesheet.Tracks.OrderBy(x => x.Position);
            if (from != null && splitPoint != null)
            {
                tracks = Cuesheet.Tracks.Where(x => x.Begin <= splitPoint.Moment && x.End >= from).OrderBy(x => x.Position);
            }
            else
            {
                if (from != null)
                {
                    tracks = Cuesheet.Tracks.Where(x => x.End >= from).OrderBy(x => x.Position);
                }
                if (splitPoint != null)
                {
                    tracks = Cuesheet.Tracks.Where(x => x.Begin <= splitPoint.Moment).OrderBy(x => x.Position);
                }
            }
            if (tracks.Any())
            {
                //Position and begin should always start from 0 even with splitpoints
                int positionDifference = 1 - Convert.ToInt32(tracks.First().Position);
                foreach (var track in tracks)
                {
                    builder.AppendLine(string.Format("{0}{1} {2:00} {3}", CuesheetConstants.Tab, CuesheetConstants.CuesheetTrack, track.Position + positionDifference, CuesheetConstants.CuesheetTrackAudio));
                    builder.AppendLine(string.Format("{0}{1}{2} \"{3}\"", CuesheetConstants.Tab, CuesheetConstants.Tab, CuesheetConstants.TrackTitle, track.Title));
                    builder.AppendLine(string.Format("{0}{1}{2} \"{3}\"", CuesheetConstants.Tab, CuesheetConstants.Tab, CuesheetConstants.TrackArtist, track.Artist));
                    if (track.Flags.Count > 0)
                    {
                        builder.AppendLine(string.Format("{0}{1}{2} {3}", CuesheetConstants.Tab, CuesheetConstants.Tab, CuesheetConstants.TrackFlags, string.Join(" ", track.Flags.Select(x => x.CuesheetLabel))));
                    }
                    if (track.PreGap.HasValue)
                    {
                        builder.AppendLine(string.Format("{0}{1}{2} {3:00}:{4:00}:{5:00}", CuesheetConstants.Tab, CuesheetConstants.Tab, CuesheetConstants.TrackPreGap, Math.Floor(track.PreGap.Value.TotalMinutes), track.PreGap.Value.Seconds, track.PreGap.Value.Milliseconds * 75 / 1000));
                    }
                    if (track.Begin.HasValue)
                    {
                        var begin = track.Begin.Value;
                        if (from != null)
                        {
                            if (from >= track.Begin)
                            {
                                begin = TimeSpan.Zero;
                            }
                            else
                            {
                                begin = track.Begin.Value - from.Value;
                            }
                        }
                        builder.AppendLine(string.Format("{0}{1}{2} {3:00}:{4:00}:{5:00}", CuesheetConstants.Tab, CuesheetConstants.Tab, CuesheetConstants.TrackIndex01, Math.Floor(begin.TotalMinutes), begin.Seconds, begin.Milliseconds * 75 / 1000));
                    }
                    else
                    {
                        throw new NullReferenceException(string.Format("{0} may not be null!", nameof(Track.Begin)));
                    }
                    if (track.PostGap.HasValue)
                    {
                        builder.AppendLine(string.Format("{0}{1}{2} {3:00}:{4:00}:{5:00}", CuesheetConstants.Tab, CuesheetConstants.Tab, CuesheetConstants.TrackPostGap, Math.Floor(track.PostGap.Value.TotalMinutes), track.PostGap.Value.Seconds, track.PostGap.Value.Milliseconds * 75 / 1000));
                    }
                }
            }
            return builder.ToString();
        }

        private String WriteExport(String? audiofileName, TimeSpan? from = null, SplitPoint? splitPoint = null)
        {
            var builder = new StringBuilder();
            if (Exportprofile != null)
            {
                var header = Exportprofile.SchemeHead
                    .Replace(Exportprofile.SchemeCuesheetArtist, splitPoint != null ? splitPoint.Artist : Cuesheet.Artist)
                    .Replace(Exportprofile.SchemeCuesheetTitle, splitPoint != null ? splitPoint.Title : Cuesheet.Title)
                    .Replace(Exportprofile.SchemeCuesheetAudiofile, audiofileName)
                    .Replace(Exportprofile.SchemeCuesheetCDTextfile, Cuesheet.CDTextfile?.Name)
                    .Replace(Exportprofile.SchemeCuesheetCatalogueNumber, Cuesheet.Cataloguenumber?.Value)
                    .Replace(Exportprofile.SchemeDate, DateTime.Now.ToShortDateString())
                    .Replace(Exportprofile.SchemeDateTime, DateTime.Now.ToString())
                    .Replace(Exportprofile.SchemeTime, DateTime.Now.ToLongTimeString());
                builder.AppendLine(header);
                IEnumerable<Track> tracks = Cuesheet.Tracks.OrderBy(x => x.Position);
                if (from != null && splitPoint != null)
                {
                    tracks = Cuesheet.Tracks.Where(x => x.Begin <= splitPoint.Moment && x.End >= from).OrderBy(x => x.Position);
                }
                else
                {
                    if (from != null)
                    {
                        tracks = Cuesheet.Tracks.Where(x => x.End >= from).OrderBy(x => x.Position);
                    }
                    if (splitPoint != null)
                    {
                        tracks = Cuesheet.Tracks.Where(x => x.Begin <= splitPoint.Moment).OrderBy(x => x.Position);
                    }
                }
                if (tracks.Any())
                {
                    //Position, Begin and End should always start from 0 even with splitpoints
                    int positionDifference = 1 - Convert.ToInt32(tracks.First().Position);
                    foreach (var track in tracks)
                    {
                        TimeSpan begin;
                        var end = track.End;
                        if (track.Begin.HasValue)
                        {
                            begin = track.Begin.Value;
                            if (from != null)
                            {
                                if (from >= track.Begin)
                                {
                                    begin = TimeSpan.Zero;
                                }
                                else
                                {
                                    begin = track.Begin.Value - from.Value;
                                }
                                end = track.End - from.Value;
                            }
                        }
                        else
                        {
                            throw new NullReferenceException(string.Format("{0} may not be null!", nameof(Track.Begin)));
                        }
                        var trackLine = Exportprofile.SchemeTracks
                            .Replace(Exportprofile.SchemeTrackArtist, track.Artist)
                            .Replace(Exportprofile.SchemeTrackTitle, track.Title)
                            .Replace(Exportprofile.SchemeTrackPosition, (track.Position + positionDifference).ToString())
                            .Replace(Exportprofile.SchemeTrackBegin, begin.ToString())
                            .Replace(Exportprofile.SchemeTrackEnd, end.ToString())
                            .Replace(Exportprofile.SchemeTrackLength, (end - begin).ToString())
                            .Replace(Exportprofile.SchemeTrackFlags, String.Join(" ", track.Flags.Select(x => x.CuesheetLabel)))
                            .Replace(Exportprofile.SchemeTrackPreGap, track.PreGap != null ? track.PreGap.Value.ToString() : String.Empty)
                            .Replace(Exportprofile.SchemeTrackPostGap, track.PostGap != null ? track.PostGap.Value.ToString() : String.Empty)
                            .Replace(Exportprofile.SchemeDate, DateTime.Now.ToShortDateString())
                            .Replace(Exportprofile.SchemeDateTime, DateTime.Now.ToString())
                            .Replace(Exportprofile.SchemeTime, DateTime.Now.ToLongTimeString());
                        builder.AppendLine(trackLine);
                    }
                }
                var footer = Exportprofile.SchemeFooter
                    .Replace(Exportprofile.SchemeCuesheetArtist, splitPoint != null ? splitPoint.Artist : Cuesheet.Artist)
                    .Replace(Exportprofile.SchemeCuesheetTitle, splitPoint != null ? splitPoint.Title : Cuesheet.Title)
                    .Replace(Exportprofile.SchemeCuesheetAudiofile, audiofileName)
                    .Replace(Exportprofile.SchemeCuesheetCDTextfile, Cuesheet.CDTextfile?.Name)
                    .Replace(Exportprofile.SchemeCuesheetCatalogueNumber, Cuesheet.Cataloguenumber?.Value)
                    .Replace(Exportprofile.SchemeDate, DateTime.Now.ToShortDateString())
                    .Replace(Exportprofile.SchemeDateTime, DateTime.Now.ToString())
                    .Replace(Exportprofile.SchemeTime, DateTime.Now.ToLongTimeString());
                builder.AppendLine(footer);
            }
            return builder.ToString();
        }

        protected override ValidationResult Validate(string property)
        {
            ValidationResult validationResult;
            switch (property)
            {
                case nameof(Cuesheet):
                    var validationResults = new Dictionary<IValidateable, ValidationResult>
                    {
                        { Cuesheet, Cuesheet.Validate() },
                        { Cuesheet.Cataloguenumber, Cuesheet.Cataloguenumber.Validate() }
                    };
                    foreach (var track in Cuesheet.Tracks)
                    {
                        validationResults.Add(track, track.Validate());
                    }
                    if (validationResults.Any(x => x.Value.Status == ValidationStatus.Error))
                    {
                        var messages = validationResults.Values.Where(x => x.ValidationMessages != null).SelectMany(x => x.ValidationMessages!);
                        validationResult = ValidationResult.Create(ValidationStatus.Error, messages);
                    }
                    else
                    {
                        validationResult = ValidationResult.Create(ValidationStatus.Success);
                    }
                    break;
                case nameof(ApplicationOptions):
                    if (ExportType == ExportType.Cuesheet)
                    {
                        if (ApplicationOptions == null)
                        {
                            var validationMessages = new List<ValidationMessage>()
                            {
                                new("{0} has no value!", nameof(ApplicationOptions))
                            };
                            validationResult = ValidationResult.Create(ValidationStatus.Error, validationMessages);
                        }
                        else
                        {
                            validationResult = ApplicationOptions.Validate(x => x.CuesheetFilename);
                        }
                    }
                    else
                    {
                        validationResult = ValidationResult.Create(ValidationStatus.NoValidation);
                    }
                    break;
                case nameof(Exportprofile):
                    if (ExportType == ExportType.Exportprofile)
                    {
                        if (Exportprofile != null)
                        {
                            validationResult = Exportprofile.Validate();
                        }
                        else
                        {
                            var validationMessages = new List<ValidationMessage>()
                            {
                                new("{0} has no value!", nameof(Exportprofile))
                            };
                            validationResult = ValidationResult.Create(ValidationStatus.Error, validationMessages);
                        }
                    }
                    else
                    {
                        validationResult = ValidationResult.Create(ValidationStatus.NoValidation);
                    }
                    break;
                default:
                    validationResult = ValidationResult.Create(ValidationStatus.NoValidation);
                    break;
            }
            return validationResult;
        }
    }
}
