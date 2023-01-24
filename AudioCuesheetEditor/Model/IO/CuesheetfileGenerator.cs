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
using System.Text;

namespace AudioCuesheetEditor.Model.IO
{
    public class CuesheetfileGenerator
    {
        public Cuesheet Cuesheet { get; private set; }

        /// <summary>
        /// Is an export of the <see cref="Cuesheet"/> possible?
        /// </summary>
        public Boolean CanWrite
        {
            get
            {
                if (Cuesheet.Validate().Status == ValidationStatus.Error)
                {
                    return false;
                }
                if (Cuesheet.Cataloguenumber.Validate().Status == ValidationStatus.Error)
                {
                    return false;
                }
                if (Cuesheet.Tracks.Any(x => x.Validate().Status == ValidationStatus.Error))
                {
                    return false;
                }
                return true;
            }
        }

        public CuesheetfileGenerator(Cuesheet cuesheet)
        {
            Cuesheet = cuesheet;
        }

        public IReadOnlyCollection<Cuesheetfile> GenerateCuesheetFiles()
        {
            //TODO: React to SplitPoints
            List<Cuesheetfile> cuesheetfiles = new();
            if (CanWrite == true)
            {
                var builder = new StringBuilder();
                if ((Cuesheet.Cataloguenumber != null) && (String.IsNullOrEmpty(Cuesheet.Cataloguenumber.Value) == false) && (Cuesheet.Cataloguenumber.Validate().Status != Entity.ValidationStatus.Error))
                {
                    builder.AppendLine(String.Format("{0} {1}", CuesheetConstants.CuesheetCatalogueNumber, Cuesheet.Cataloguenumber.Value));
                }
                if (Cuesheet.CDTextfile != null)
                {
                    builder.AppendLine(String.Format("{0} \"{1}\"", CuesheetConstants.CuesheetCDTextfile, Cuesheet.CDTextfile.FileName));
                }
                builder.AppendLine(String.Format("{0} \"{1}\"", CuesheetConstants.CuesheetTitle, Cuesheet.Title));
                builder.AppendLine(String.Format("{0} \"{1}\"", CuesheetConstants.CuesheetArtist, Cuesheet.Artist));
                builder.AppendLine(String.Format("{0} \"{1}\" {2}", CuesheetConstants.CuesheetFileName, Cuesheet.Audiofile?.Name, Cuesheet.Audiofile?.AudioFileType));
                foreach (var track in Cuesheet.Tracks)
                {
                    builder.AppendLine(String.Format("{0}{1} {2:00} {3}", CuesheetConstants.Tab, CuesheetConstants.CuesheetTrack, track.Position, CuesheetConstants.CuesheetTrackAudio));
                    builder.AppendLine(String.Format("{0}{1}{2} \"{3}\"", CuesheetConstants.Tab, CuesheetConstants.Tab, CuesheetConstants.TrackTitle, track.Title));
                    builder.AppendLine(String.Format("{0}{1}{2} \"{3}\"", CuesheetConstants.Tab, CuesheetConstants.Tab, CuesheetConstants.TrackArtist, track.Artist));
                    if (track.Flags.Count > 0)
                    {
                        builder.AppendLine(String.Format("{0}{1}{2} {3}", CuesheetConstants.Tab, CuesheetConstants.Tab, CuesheetConstants.TrackFlags, String.Join(" ", track.Flags.Select(x => x.CuesheetLabel))));
                    }
                    if (track.PreGap.HasValue)
                    {
                        builder.AppendLine(String.Format("{0}{1}{2} {3:00}:{4:00}:{5:00}", CuesheetConstants.Tab, CuesheetConstants.Tab, CuesheetConstants.TrackPreGap, Math.Floor(track.PreGap.Value.TotalMinutes), track.PreGap.Value.Seconds, (track.PreGap.Value.Milliseconds * 75) / 1000));
                    }
                    if (track.Begin.HasValue)
                    {
                        builder.AppendLine(String.Format("{0}{1}{2} {3:00}:{4:00}:{5:00}", CuesheetConstants.Tab, CuesheetConstants.Tab, CuesheetConstants.TrackIndex01, Math.Floor(track.Begin.Value.TotalMinutes), track.Begin.Value.Seconds, (track.Begin.Value.Milliseconds * 75) / 1000));
                    }
                    if (track.PostGap.HasValue)
                    {
                        builder.AppendLine(String.Format("{0}{1}{2} {3:00}:{4:00}:{5:00}", CuesheetConstants.Tab, CuesheetConstants.Tab, CuesheetConstants.TrackPostGap, Math.Floor(track.PostGap.Value.TotalMinutes), track.PostGap.Value.Seconds, (track.PostGap.Value.Milliseconds * 75) / 1000));
                    }
                }
                cuesheetfiles.Add(new Cuesheetfile() { Content = Encoding.UTF8.GetBytes(builder.ToString()) });
            }
            return cuesheetfiles;
        }
    }
}
