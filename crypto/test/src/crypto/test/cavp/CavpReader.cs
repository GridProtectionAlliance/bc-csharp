﻿using System;
using System.Collections.Generic;
using System.IO;

using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.Collections;
using Org.BouncyCastle.Utilities.Encoders;
using Org.BouncyCastle.Utilities.Test;

namespace Org.BouncyCastle.Crypto.Tests.Cavp
{
    internal class Vector : Dictionary<string, object>
    {
        private IDictionary<string, string> mHeader = null;

        public Vector(IDictionary<string, string> header)
        {
            this.mHeader = header;
        }

        public string ValueAsString(string name)
        {
            return this[name] as string;
        }

        public string HeaderAsString(string name)
        {
            return CollectionUtilities.GetValueOrNull(mHeader, name);
        }

        public byte[] ValueAsBytes(string name)
        {
            string value = this[name] as string;
            if (value != null)
            {
                return Hex.Decode(value);
            }

            return null;
        }

        public byte[] HeaderAsBytes(string name)
        {
            string value = HeaderAsString(name);
            return value == null ? null : Hex.Decode(value);
        }

        public int ValueAsInt(string name)
        {
            string value = this[name] as string;
            if (value == null)
                throw new InvalidOperationException(name + " was null");

            return int.Parse(value);
        }

        public int ValueAsInt(string name, int def)
        {
            string value = this[name] as string;
            if (value == null)
                return def;

            return int.Parse(value);
        }
    }

    internal class CavpReader
    {
        public static IList<Vector> ReadVectorFile(string name)
        {
            var vectors = new List<Vector>();
            IDictionary<string, string> header = null;
            Vector currentVector = null;

            int headerState = 0;

            using (StreamReader r = new StreamReader(SimpleTest.GetTestDataAsStream("crypto.cavp." + name)))
            {
                string line;
                while (null != (line = r.ReadLine()))
                {
                    // Reading a header or waiting to encounter a header
                    // and we encounter a vector line.
                    // Set up a new vector.
                    if (headerState <= 1 && !line.StartsWith("[") && Contains(line, '='))
                    {
                        currentVector = new Vector(header);
                        vectors.Add(currentVector);
                        headerState = 2;
                    }

                    //
                    // Read
                    //
                    if (headerState == 2)
                    {
                        //
                        // Header line found immediately after vector elements.
                        //
                        if (line.StartsWith("[") && line.EndsWith("]"))
                        {
                            headerState = 0;
                        }
                        else

                            //
                            // Not a valid line so we assume this is a break between vectors.
                            //
                            if (headerState == 2 && !Contains(line, '='))
                            {
                                headerState = 0;
                            }
                            else

                                //
                                // Vector parameter.
                                //
                                if (!line.StartsWith("[") && Contains(line, '='))
                                {
                                    if (currentVector == null)
                                    {
                                        currentVector = new Vector(header);
                                        vectors.Add(currentVector);
                                    }

                                    string[] parts = line.Split('=');
                                    currentVector[parts[0].Trim()] = parts[1].Trim();
                                    headerState = 2;
                                }
                    }

                    //
                    // Found start of header block.
                    // We need a new header map.
                    //
                    if (headerState == 0 && line.StartsWith("[") && line.EndsWith("]"))
                    {
                        header = new Dictionary<string, string>();
                        headerState = 1;
                    }

                    //
                    // Read header lines.
                    //
                    if (headerState <= 1)
                    {
                        if (line.StartsWith("[") && line.EndsWith("]"))
                        {
                            // Strip away brackets.
                            string trimmed = line.Substring(1, line.Length - 2);
                            string[] parts = trimmed.Split('=');
                            header[parts[0].Trim()] = parts[1].Trim();
                            headerState = 1;
                        }
                    }
                }
            }

            return vectors;
        }

        public static IMac CreatePrf(Vector config)
        {
            string type = config.HeaderAsString("PRF");
            if (type == null)
                throw new ArgumentException("PRF field was null.");

            return MacUtilities.GetMac(GetMacForPrf(type));
        }

        private static bool Contains(string s, char c)
        {
            return s.IndexOf(c) >= 0;
        }

        private static string GetMacForPrf(string type)
        {
            if (type.StartsWith("CMAC_AES"))
            {
                return "AESCMAC";
            }
            else if (type.StartsWith("CMAC_TDES"))
            {
                return "DESEDECMAC";
            }
            else if (type.StartsWith("HMAC_SHA1"))
            {
                return "HMAC/SHA1";
            }
            else if (type.StartsWith("HMAC_SHA224"))
            {
                return "HMAC/SHA224";
            }
            else if (type.StartsWith("HMAC_SHA256"))
            {
                return "HMAC/SHA256";
            }
            else if (type.StartsWith("HMAC_SHA384"))
            {
                return "HMAC/SHA384";
            }
            else if (type.StartsWith("HMAC_SHA512"))
            {
                return "HMAC/SHA512";
            }
            else
            {
                throw new ArgumentException("Unknown Mac for PRF " + type);
            }
        }
    }
}
