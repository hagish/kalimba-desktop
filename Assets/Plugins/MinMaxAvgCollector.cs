using UnityEngine;
using System;

public class MinMaxAvgCollector
{
	private float min;
	private float max;
	private float sum;
	private int count;
	
	public MinMaxAvgCollector ()
	{		
		ClearData();
	}
	
	public void Reset()
	{
		ClearData();
	}
	
	private void ClearData()
	{
		count = 0;
		min = 0;
		max = 0;
		sum = 0;
	}
	
	public void PutInNumber(float x)
	{
		if (count == 0)
		{
			min = x;
			max = x;
			sum = x;
			count = 1;
		}
		else
		{
			min = Math.Min(min, x);	
			max = Math.Max(max, x);
			count++;
			sum += x;
		}
	}
	
	public float GetMin()
	{
		return min;
	}
		
	public float GetMax()
	{
		return max;
	}
	
	public float GetAvg()
	{
		return sum / (float)count;
	}
	
	public int GetCount()
	{
		return count;	
	}

	public override string ToString ()
	{
		return string.Format ("[min={0} avg={1} max={2} count={3}]", GetMin(), GetMax(), GetAvg(), GetCount());
	}
}
