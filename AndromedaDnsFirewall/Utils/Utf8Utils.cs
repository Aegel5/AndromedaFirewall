using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AndromedaDnsFirewall.Utils;

internal static class Utf8Utils {

	public static IEnumerable<byte[]> Split(byte[] utf8, string separators) {
		return Split(utf8, Encoding.UTF8.GetBytes(separators));
	}

	// разделить только asci!
	public static IEnumerable<byte[]> Split(byte[] utf8, byte[] separators) {

		int pos = 0;
		while (true) {
			int index = utf8.AsSpan(pos).IndexOfAny(separators);
			if (index < 0) break;

			if (index > 0) {
				yield return utf8.AsSpan(pos, index).ToArray();
			}

			pos += index + 1;
		}

		if (pos < utf8.Length) {
			yield return utf8.AsSpan(pos).ToArray();
		}

	}
	public static IEnumerable<byte[]> Split(byte[] utf8, byte separator) {

		int pos = 0;
		while (true) {
			int index = utf8.AsSpan(pos).IndexOf(separator);
			if (index < 0) break;

			if (index > 0) {
				yield return utf8.AsSpan(pos, index).ToArray();
			}

			pos += index + 1;
		}

		if (pos < utf8.Length) {
			yield return utf8.AsSpan(pos).ToArray();
		}

	}

	public static void Split(ReadOnlySpan<byte> utf8, byte separator, Action<ReadOnlySpan<byte>> action) {

		int pos = 0;
		while (true) {
			int index = utf8.Slice(pos).IndexOf(separator);
			if (index < 0) break;

			if (index > 0) {
				action(utf8.Slice(pos, index));
			}

			pos += index + 1;
		}

		if (pos < utf8.Length) {
			action(utf8.Slice(pos));
		}

	}

	public static void Split(ReadOnlySpan<byte> utf8, byte[] separator, Action<ReadOnlySpan<byte>> action) {

		int pos = 0;
		while (true) {
			int index = utf8.Slice(pos).IndexOfAny(separator);
			if (index < 0) break;

			if (index > 0) {
				action(utf8.Slice(pos, index));
			}

			pos += index + 1;
		}

		if (pos < utf8.Length) {
			action(utf8.Slice(pos));
		}

	}


	async public static void SplitAsync(Stream stream, byte separator, Action<ReadOnlySpan<byte>> action) {

		byte[] buffer = new byte[8192];
		int offset = 0; // сколько байт осталось с прошлого чтения

		while (true) {
			// Читаем в свободную часть массива после остатка (offset)
			int bytesRead = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset));
			if (bytesRead == 0) break;

			int totalInBuffered = offset + bytesRead;
			int start = 0;

			// Обрабатываем все найденные сепараторы в текущем окне данных
			while (true) {
				// Ищем разделитель в оставшейся части буфера
				int index = Array.IndexOf(buffer, separator, start, totalInBuffered - start);

				if (index == -1) break; // Больше разделителей в этом куске нет

				// Вызываем callback для данных ПЕРЕД разделителем
				action(buffer.AsSpan(start, index - start));

				start = index + 1; // Прыгаем за разделитель
			}

			// Вычисляем, сколько байт осталось после последнего найденного разделителя
			int leftover = totalInBuffered - start;

			if (leftover > 0) {
				// Если вся память забита одной длинной строкой без разделителя — расширяемся
				if (start == 0 && totalInBuffered == buffer.Length) {
					Array.Resize(ref buffer, buffer.Length * 2);
					offset = totalInBuffered;
				} else {
					// Сдвигаем остаток в начало массива для следующей итерации
					Buffer.BlockCopy(buffer, start, buffer, 0, leftover);
					offset = leftover;
				}
			} else {
				offset = 0;
			}
		}

		// Если поток закрылся, а в буфере остались данные (последний кусок без \n в конце)
		if (offset > 0) {
			action(buffer.AsSpan(0, offset));
		}
	}
}

