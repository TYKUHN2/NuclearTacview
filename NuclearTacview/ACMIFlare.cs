﻿using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace NuclearTacview
{
    internal class ACMIFlare: ACMIObject
    {
        private static int FLAREID = 0;

        private Vector3 lastPos = new(float.NaN, float.NaN, float.NaN);

        public readonly IRFlare flare;

        public ACMIFlare(IRSource source): base((Interlocked.Increment(ref FLAREID) & int.MaxValue) | (1 << 32))
        {
            if (source.flare == false)
                throw new ArgumentException("IRSource is not flare");

            flare = source.transform.gameObject.GetComponent<IRFlare>();
            if (flare == null)
                throw new InvalidOperationException("Failed to acquire IRFlare");
        }

        public override Dictionary<string, string> Init()
        {
            return new()
            {
                { "Type", "Misc+Decoy+Flare" }
            };
        }

        public override Dictionary<string, string> Update()
        {
            Dictionary<string, string> props = [];

            float fx = MathF.Round(flare.transform.position.GlobalX(), 2);
            float fy = MathF.Round(flare.transform.position.GlobalY(), 2);
            float fz = MathF.Round(flare.transform.position.GlobalZ(), 2);

            Vector3 newPos = new(fx, fy, fz);

            if (newPos != lastPos)
            {
                props.Add("T", UpdatePosition(newPos));

                lastPos = newPos;
            }

            return props;
        }
        private string UpdatePosition(Vector3 newPos)
        {
            string x = Mathf.Approximately(newPos.x, lastPos.x) ? "" : newPos.x.ToString();
            string y = Mathf.Approximately(newPos.y, lastPos.y) ? "" : newPos.y.ToString();
            string z = Mathf.Approximately(newPos.z, lastPos.z) ? "" : newPos.z.ToString();

            (float latitude, float longitude) = CartesianToGeodetic(newPos.x, newPos.z);

            return $"{longitude}|{latitude}|{y}|{x}|{z}";
        }

        (float, float) CartesianToGeodetic(float U /* X */, float V /* Z */)
        {
            //Stupid simplification but it works.
            float longArc = (float)Math.PI * 6378137;
            float latArc = longArc / 2;

            float latitude = V * 90 / latArc;
            float longitude = U * 180 / longArc;

            return (latitude, longitude);
        }
    }
}
