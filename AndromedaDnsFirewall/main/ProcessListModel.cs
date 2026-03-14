using AndromedaDnsFirewall.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace AndromedaDnsFirewall.main; 
internal static class ProcessListModel {

	static readonly ObservableCollection<ProcessInfoModel> list = new();

	// выходная дорожка из класса
	public static IEnumerable<ProcessInfoModel> ModelBinding => list;

	// входная и единственная дорожка в класс
	static public void NotifyChanged(ProcessInfoModel info) {
		// Пока: 1) храним последние 70 2) прекидываем наверх при обновлении. 3) поиск - полный проход
		for (int i = 0; i < list.Count; i++) {
			if (ReferenceEquals(list[i], info)) {
				if (i <= 5) {
					// просто тригерем обновление
					list[i] = info;
					return;
				} else {
					list.RemoveAt(i);
					break;
				}
			}
		}
		list.Insert(0, info);
		while (list.Count > 70) {
			// удаляем старые
			list.RemoveAt(list.Count - 1);
		}
	}
}
