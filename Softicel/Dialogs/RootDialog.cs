using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Softicel.Dialogs
{
	[Serializable]
	public class RootDialog : IDialog<object>
	{
		public string Responsabil { get; set; }
		public string Restaurant { get; set; }
		public Dictionary<string, string> order = new Dictionary<string, string>();
		public Task StartAsync(IDialogContext context)
		{
			context.Wait(MessageReceivedAsync);

			return Task.CompletedTask;
		}

		private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
		{
			var activity = await result as Activity;
			
			// calculate something for us to return
			int length = (activity.Text ?? string.Empty).Length;
			if(!string.IsNullOrEmpty(activity.Text))
			{
				activity.Text = Regex.Replace(activity.Text,"@Softicel","").Trim();
				var array = activity.Text.Split(' ');
				if(array[0].Equals("hi",StringComparison.InvariantCultureIgnoreCase) || 
					array[0].Equals("hello", StringComparison.InvariantCultureIgnoreCase) ||
					array[0].Equals("salut",StringComparison.InvariantCultureIgnoreCase) ||
					array[0].Equals("noroc",StringComparison.InvariantCultureIgnoreCase))
				{
					var answers = new string[] {"Zdarova", "Privet", "Salut", "Noroc", "Hi minions (hug) ", "Iar vreti mincare? (unamused) "};
					await context.PostAsync( answers[new Random().Next(0,6)]);
				}
				else if (array[0].Equals("responsabil", StringComparison.InvariantCultureIgnoreCase) && array.Length>1)
				{
					Responsabil = array.Skip(1).Aggregate((a, b) => a + " " + b);
					await context.PostAsync($"Responsabil de comanda e {Responsabil}");
				}

				else if (array[0].Equals("restaurant", StringComparison.InvariantCultureIgnoreCase) && array.Length > 1)
				{
					Restaurant = array.Skip(1).Aggregate((a, b) => a + " " + b);
					await context.PostAsync($"Restaurant setat : {Restaurant}");
				}

				else if(activity.Text.Equals("cine da comanda?",StringComparison.InvariantCultureIgnoreCase)
					|| activity.Text.Equals("cine?", StringComparison.InvariantCultureIgnoreCase))
				{
					if(!string.IsNullOrEmpty(Responsabil))
						await context.PostAsync($"Comanda o va face {Responsabil}");
					else
						await context.PostAsync("Nu avem responsabil pe mincare :( ");
				}

				else if(activity.Text.Equals("unde?",StringComparison.InvariantCultureIgnoreCase)
					|| activity.Text.Equals("unde dam comanda?", StringComparison.InvariantCultureIgnoreCase))
				{
					if (!string.IsNullOrEmpty(Restaurant))
						await context.PostAsync($"Comandam mincare de la {Restaurant}");
					else
						await context.PostAsync("Restaurantul nu a fost ales :( ");
				}

				else if(array[0].Equals("adauga",StringComparison.InvariantCultureIgnoreCase) &&  array.Length>2)
				{
					if(order.Keys.Contains(array[1]))
					{
						order[array[1]] += "," + array.Skip(2).Aggregate((a, b) => a + " " + b);
					}
					else
					{
						order.Add(array[1], array.Skip(2).Aggregate((a, b) => a + " " + b));
					}
					await context.PostAsync($"Adaugat {array.Skip(2).Aggregate((a, b) => a + " " + b)} pentru {array[1]}");
				}

				else if(array[0].Equals("sterge", StringComparison.InvariantCultureIgnoreCase) && array.Length> 2)
				{
					if (order.Keys.Contains(array[1]))
					{
						var list = order[array[1]].Split(',').ToList();
						if (list.Contains(array.Skip(2).Aggregate((a, b) => a + " " + b)))
						{
							list.Remove(array.Skip(2).Aggregate((a, b) => a + " " + b));
							if(list.Count > 0)
							{
								order[array[1]] = list.Aggregate((a, b) => a + "," + b);
								await context.PostAsync($"Din comanda lui {array[1]} a fost stearsa : {array.Skip(2).Aggregate((a, b) => a + " " + b)}");
							}
							else
							{
								order.Remove(array[1]);
								await context.PostAsync($"Din comanda lui {array[1]} a fost stearsa : {array.Skip(2).Aggregate((a, b) => a + " " + b)}");
							}
						}
						else
							await context.PostAsync($"{array[1]} nu a comandat {array.Skip(2).Aggregate((a, b) => a + " " + b)}");
					}
					else
						await context.PostAsync($"{array[1]} nu a comanda nimic");
				}

				else if(activity.Text.Equals("arata lista",StringComparison.InvariantCultureIgnoreCase))
				{
					var message = $"List actuala a comenzilor:<br/>{string.Join("," + "<br/>", order.Select(kvp => kvp.Key + " = " + kvp.Value))}";
					await context.PostAsync(message);
				}

				else if (activity.Text.Equals("reset", StringComparison.InvariantCultureIgnoreCase))
				{
					await context.PostAsync("Ești sigur că vrei să resetezi comenzile? (worry) Daca da scrie HARD RESET");
				}

				else if (activity.Text.Equals("hard reset",StringComparison.InvariantCultureIgnoreCase))
				{
					this.Responsabil = string.Empty;
					this.Restaurant = string.Empty;
					this.order = new Dictionary<string, string>();
					await context.PostAsync("Comenzile au fost resetate :P ");
				}

				else if(activity.Text.Equals("help", StringComparison.InvariantCultureIgnoreCase))
				{
					await context.PostAsync($"Eu te pot ajuta cu urmatoarele comenzi (comenzile mele nu sunt Case Sensitive :P ) :" +
						$"<br/>Responsabil [numele persoanei] - seteaza persoana responsabila" +
						$"<br/>Restaurant [numele restaurant] - seteaza restaurantul pentru comanda" +
						$"<br/>Cine? sau Cine da comanada? - iti voi spune cine este responsabil de comanda (nod) " +
						$"<br/>Unde? sau Unde dam comanda? - iti voi spunde de unde vom comanda mincare (pi) " +
						$"<br/>Adauga [numele tau] [mincarea aleasa]" +
						$"<br/>Sterge [numele tau] [mincarea aleasa]" +
						$"<br/>Arata lista - iti voi arata lista actuala a comenzii (cake) (pi) (cheese) (drink) (turkey) " +
						$"<br/>Sumeaza comanda - totalizarea comenzii" + 
						$"<br/>Reset - voi incerca sa sterg lista comenzilor, necesita confirmare" +
						$"<br/>Hard reset - sterg fara confirmare (bandit)" 
						);
				}

				else if(activity.Text.Equals("sumeaza comanda",StringComparison.InvariantCultureIgnoreCase))
				{
					var comanda = new Dictionary<string, int>();
					if (order.Values.Count > 0)
					{
						foreach (var item in order.Values.Aggregate((a, b) => a + "," + b).Split(','))
						{
							if (comanda.ContainsKey(item))
								comanda[item]++;
							else
								comanda.Add(item, 1);
						}
						var message = $"Face comanda - {Responsabil} de la -  {Restaurant} <br/>Totalul comenzii:<br/>{string.Join(", <br/>", comanda.Select(kvp => kvp.Value + " X " + kvp.Key))}";
						await context.PostAsync(message);
					}
					else
						await context.PostAsync("Comanda este goala");
				}

				else if(activity.Text.Equals("fuck you", StringComparison.InvariantCultureIgnoreCase))
				{
					await context.PostAsync("(mooning)");
				}

				else
				{
					await context.PostAsync($"Sorry, nu am inteles :^) ");
				}

			}
			// return our reply to the user
			context.Wait(MessageReceivedAsync);
		}
	}
}