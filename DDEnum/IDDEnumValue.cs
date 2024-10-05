using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DDEnum
{
	public interface IDDEnumValue<T, TValue> : IEquatable<IDDEnumValue<T, TValue>> 
		where T : DDEnumAssetBase<T>
		where TValue : struct, IDDEnumValue<T, TValue>
	{
		/// <summary>
		/// Get index of value
		/// </summary>
		public int Value { get; set; }
		
		/// <summary>
		/// Get bit value (bit in <see cref="IDDEnumMask{T,TValue}"/>)
		/// </summary>
		public long BitValue { get; }
		
		/// <summary>
		/// Get bit value (bit in <see cref="IDDEnumMask{T,TValue}"/>)
		/// </summary>
		protected static long GetBitValue(IDDEnumValue<T, TValue> value) => 1L << value.Value;
	}
	
	public interface IDDEnumMask<T, TValue, TMaskValue> : IEquatable<IDDEnumMask<T, TValue, TMaskValue>>, IEnumerable<TValue>
		where T : DDEnumAssetBase<T>
		where TValue : struct, IDDEnumValue<T, TValue>
		where TMaskValue : struct, IDDEnumMask<T, TValue, TMaskValue>
	{
		/// <summary>
		/// Get value of mask
		/// </summary>
		public long Value { get; set; }
		
		/// <summary>
		/// Enumerator of <see cref="IDDEnumMask{T,TValue}"/> that returns <see cref="IDDEnumValue{T}"/>
		/// </summary>
		public struct SetValuesEnumerator : IEnumerator<TValue>
		{
			private readonly long m_mask;
			private readonly int m_max;
			private int m_current;

			public SetValuesEnumerator(long mask, int max = 63)
			{
				m_mask = mask;
				m_max = Mathf.Min(max, 63);
				m_current = -1;
			}

			public SetValuesEnumerator GetEnumerator() => this;

			public bool MoveNext()
			{
				while (m_current < m_max)
				{
					if ((m_mask & (1L << ++m_current)) == 0L)
						continue;
				
					return true;
				}

				return false;
			}

			public void Reset() => m_current = -1;

			public TValue Current
			{
				get
				{
					TValue output = default;
					output.Value = m_current;
					return output;
				}
			}

			object IEnumerator.Current => Current;

			void IDisposable.Dispose() { }
		}
	}

	public static class IDDEnumMaskExtensions
	{
		/// <returns>True if <paramref name="mask"/> contains <paramref name="value"/></returns>
		public static bool Contains<T, TValue, TMask>(this IDDEnumMask<T, TValue, TMask> mask, TValue value)
			where T : DDEnumAssetBase<T>
			where TValue :  struct, IDDEnumValue<T, TValue>
			where TMask : struct, IDDEnumMask<T, TValue, TMask>
		{
			return (mask.Value & value.BitValue) != 0L;
		}
		
		/// <returns>True if <paramref name="mask"/> is superset of <paramref name="other"/></returns> fully
		public static bool IsSuperset<T, TValue, TMask>(this TMask mask, IDDEnumMask<T, TValue, TMask> other) 
			where T : DDEnumAssetBase<T> 
			where TValue :  struct, IDDEnumValue<T, TValue>
			where TMask : struct, IDDEnumMask<T, TValue, TMask>
		{
			return mask.Intersect(other) == other.Value;
		}
		
		/// <returns>True if <paramref name="mask"/> is proper superset of <paramref name="other"/></returns> fully
		public static bool IsProperSuperset<T, TValue, TMask>(this TMask mask, IDDEnumMask<T, TValue, TMask> other) 
			where T : DDEnumAssetBase<T> 
			where TValue :  struct, IDDEnumValue<T, TValue>
			where TMask : struct, IDDEnumMask<T, TValue, TMask>
		{
			return mask.Value != other.Value && mask.IsSuperset(other);
		}
		
		/// <returns>True if <paramref name="mask"/> is subset of <paramref name="other"/></returns> fully
		public static bool IsSubset<T, TValue, TMask>(this TMask mask, IDDEnumMask<T, TValue, TMask> other) 
			where T : DDEnumAssetBase<T> 
			where TValue :  struct, IDDEnumValue<T, TValue>
			where TMask : struct, IDDEnumMask<T, TValue, TMask>
		{
			return mask.Intersect(other) == mask.Value;
		}
		
		/// <returns>True if <paramref name="mask"/> is proper subset of <paramref name="other"/></returns> fully
		public static bool IsProperSubset<T, TValue, TMask>(this TMask mask, IDDEnumMask<T, TValue, TMask> other) 
			where T : DDEnumAssetBase<T> 
			where TValue :  struct, IDDEnumValue<T, TValue>
			where TMask : struct, IDDEnumMask<T, TValue, TMask>
		{
			return mask.Value != other.Value && mask.IsSubset(other);
		}
		
		/// <returns>True if <paramref name="mask"/> overlaps <paramref name="other"/></returns>
		public static bool Overlaps<T, TValue, TMask>(this TMask mask, IDDEnumMask<T, TValue, TMask> other)
			where T : DDEnumAssetBase<T> 
			where TValue :  struct, IDDEnumValue<T, TValue>
			where TMask : struct, IDDEnumMask<T, TValue, TMask>
		{
			return mask.Intersect(other) != 0L;
		}

		private static long Intersect<T, TValue, TMask>(this TMask mask, IDDEnumMask<T, TValue, TMask> other) 
			where T : DDEnumAssetBase<T> 
			where TValue :  struct, IDDEnumValue<T, TValue>
			where TMask : struct, IDDEnumMask<T, TValue, TMask>
		{
			return mask.Value & other.Value;
		}
		
		/// <returns>New <typeparamref name="TMask"/> with an intersection of <paramref name="mask"/> and <paramref name="other"/></returns>
		public static TMask Intersection<T, TValue, TMask>(this TMask mask, IDDEnumMask<T, TValue, TMask> other) 
			where T : DDEnumAssetBase<T> 
			where TValue : struct, IDDEnumValue<T, TValue>
			where TMask : struct, IDDEnumMask<T, TValue, TMask>
		{
			TMask output = default; 
			
			output.Value = mask.Intersect(other);

			return output;
		}
		
		/// <returns>New <typeparamref name="TMask"/> with a union of <paramref name="mask"/> and <paramref name="other"/></returns>
		public static TMask Union<T, TValue, TMask>(this TMask mask, IDDEnumMask<T, TValue, TMask> other) 
			where T : DDEnumAssetBase<T> 
			where TValue : struct, IDDEnumValue<T, TValue>
			where TMask : struct, IDDEnumMask<T, TValue, TMask>
		{
			TMask output = default; 
			
			output.Value = mask.Value | other.Value;

			return output;
		}
		
		/// <returns>New <typeparamref name="TMask"/> with an elements that are only in <paramref name="mask"/> or <paramref name="other"/></returns>
		public static TMask SymmetricalExceptWith<T, TValue, TMask>(this TMask mask, IDDEnumMask<T, TValue, TMask> other) 
			where T : DDEnumAssetBase<T> 
			where TValue : struct, IDDEnumValue<T, TValue>
			where TMask : struct, IDDEnumMask<T, TValue, TMask>
		{
			TMask output = default; 
			
			output.Value = mask.Value ^ other.Value;

			return output;
		}
		
		/// <returns>New <typeparamref name="TMask"/> with an elements that are in <paramref name="mask"/> but not in <paramref name="other"/></returns>
		public static TMask ExceptWith<T, TValue, TMask>(this TMask mask, IDDEnumMask<T, TValue, TMask> other) 
			where T : DDEnumAssetBase<T> 
			where TValue : struct, IDDEnumValue<T, TValue>
			where TMask : struct, IDDEnumMask<T, TValue, TMask>
		{
			TMask output = default; 
			
			output.Value = mask.Value & ~other.Value;

			return output;
		}
		
		/// <returns>New <typeparamref name="TMask"/> with <paramref name="value"/> added</returns>
		public static TMask Add<T, TValue, TMask>(this IDDEnumMask<T, TValue, TMask> mask, TValue value)
			where T : DDEnumAssetBase<T> 
			where TValue :  struct, IDDEnumValue<T, TValue>
			where TMask : struct, IDDEnumMask<T, TValue, TMask>
		{
			TMask output = default;
			
			output.Value = mask.Value | value.BitValue;

			return output;
		}
		
		/// <returns>New <typeparamref name="TMask"/> with <paramref name="value"/> removed</returns>
		public static TMask Remove<T, TValue, TMask>(this IDDEnumMask<T, TValue, TMask> mask, TValue value) 
			where T : DDEnumAssetBase<T>
			where TValue :  struct, IDDEnumValue<T, TValue>
			where TMask : struct, IDDEnumMask<T, TValue, TMask>
		{
			TMask output = default;
			
			output.Value = mask.Value & ~value.BitValue;

			return output;
		}
		
		/// <returns>True if any bit is set in <paramref name="mask"/></returns>
		public static bool AnySet<T, TValue, TMask>(this IDDEnumMask<T, TValue, TMask> mask) 
			where T : DDEnumAssetBase<T>
			where TValue :  struct, IDDEnumValue<T, TValue>
			where TMask : struct, IDDEnumMask<T, TValue, TMask>
		{
			return (mask.Value & DDEnumAssetBase<T>.Instance.SetValuesMask) != 0;
		}

		/// <returns>Enumerator of <typeparamref name="TValue"/> of all set bits in <paramref name="mask"/></returns>
		public static IEnumerator<TValue> GetValuesEnumerator<T, TValue, TMask>(this IDDEnumMask<T, TValue, TMask> mask)
			where T : DDEnumAssetBase<T>
			where TValue :  struct, IDDEnumValue<T, TValue>
			where TMask : struct, IDDEnumMask<T, TValue, TMask>
		{
			return new IDDEnumMask<T,TValue,TMask>.SetValuesEnumerator(mask.Value, DDEnumAssetBase<T>.Instance.MaxValueIndex);
		}

		/// <returns>New <typeparamref name="TValue"/> with next value counting from <paramref name="value"/></returns>
		public static TValue Incremented<T, TValue>(this IDDEnumValue<T, TValue> value)
			where T : DDEnumAssetBase<T>
			where TValue : struct, IDDEnumValue<T, TValue>
		{
			return ChangeToNextValue<T, TValue>(value.Value, DDEnumAssetBase<T>.Instance.SetValuesMask);
		}

		/// <returns>New <typeparamref name="TValue"/> with previous value counting from <paramref name="value"/></returns>
		public static TValue Decremented<T, TValue>(this IDDEnumValue<T, TValue> value)
			where T : DDEnumAssetBase<T>
			where TValue : struct, IDDEnumValue<T, TValue>
		{
			return ChangeToNextValue<T, TValue>(value.Value, DDEnumAssetBase<T>.Instance.SetValuesMask, false);
		}

		/// <returns>New <typeparamref name="TValue"/> with next value counting from <paramref name="value"/> in <paramref name="mask"/></returns>
		public static TValue Incremented<T, TValue, TMask>(this TValue value, IDDEnumMask<T, TValue, TMask> mask)
			where T : DDEnumAssetBase<T>
			where TValue : struct, IDDEnumValue<T, TValue>
			where TMask : struct, IDDEnumMask<T, TValue, TMask>
		{
			return ChangeToNextValue<T, TValue>(value.Value, mask.Value);
		}

		/// <returns>New <typeparamref name="TValue"/> with previous value counting from <paramref name="value"/> in <paramref name="mask"/></returns>
		public static TValue Decremented<T, TValue, TMask>(this TValue value, IDDEnumMask<T, TValue, TMask> mask)
			where T : DDEnumAssetBase<T>
			where TValue : struct, IDDEnumValue<T, TValue>
			where TMask : struct, IDDEnumMask<T, TValue, TMask>
		{
			return ChangeToNextValue<T, TValue>(value.Value, mask.Value, false);
		}

		private static TValue ChangeToNextValue<T, TValue>(int value, long mask, bool increment = true)
			where T : DDEnumAssetBase<T>
			where TValue : struct, IDDEnumValue<T, TValue>
		{
			var count = DDEnumAssetBase<T>.Instance.MaxValueIndex + 1;
			var multiplier = increment ? 1 : -1;
			
			for (int i = 0; i <= count; i++)
			{
				int index = (value + multiplier * (i + 1)) % count;

				if ((mask & (1 << index)) == 0) 
					continue;
				
				value = index;
				break;
			}
			
			TValue output = default;
			output.Value = value;
			
			return output;
		}
	}
}