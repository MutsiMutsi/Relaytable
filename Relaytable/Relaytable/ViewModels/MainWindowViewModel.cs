using Relaytable.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Relaytable.ViewModels
{
	public class MainWindowViewModel : ViewModelBase
	{
		public string Greeting { get; } = "Welcome to Avalonia!";

		public ObservableCollection<NodeNeighbour> Neighbours { get; set; }

		public MainWindowViewModel()
		{
			var neighbours = new List<NodeNeighbour>();
			Neighbours = new ObservableCollection<NodeNeighbour>(neighbours);
		}
	}
}
