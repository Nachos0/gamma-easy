// GAMMA Easy 0.1.0. Copyright (C) 2026. GPL-3.0-or-later.
// Independent community launcher. Not an official GAMMA or GSC product.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

class DriveChoice
{
    public string Root;
    public string Text;
    public override string ToString() { return Text; }
}
