using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CIS
{
	public class AutoBuildRawTemplate : AutoBuildBaseRawTemplate {
		//add other field here, make sure they are public
		//public Color OtherField;//demonstrate here, please remove it 

		public override void ApplyRawConfigData(string channelname)
		{
			//Apply your own data into game
			base.ApplyRawConfigData(channelname);
		}

		public static void ClearRawConfigData()
		{
			
		}


	}
}

