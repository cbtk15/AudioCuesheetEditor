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
using AudioCuesheetEditor.Model.IO.Audio;
using System;
using System.Threading.Tasks;

namespace AudioCuesheetEditorTests.Utility
{
    internal class AudioConverterServiceUnitTest : IAudioConverterService
    {
        public Task<byte[]?> SplitAudiofileAsync(Audiofile audiofile, TimeSpan from, TimeSpan? to = null)
        {
            // This implementation does nothing with audio processing, so we only return some fake data
            return Task.FromResult<byte[]?>(null);
        }
    }
}
