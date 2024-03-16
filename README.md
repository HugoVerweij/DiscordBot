# Discord bot.
Deze repo functioneert als een voorbeeld applicatie van een van de diverse discord bots die ik heb ontwikkeld. Het is geschreven met behulp van [Discord.Net](https://github.com/discord-net/Discord.Net), een API wrapper voor de officiele Discord API.

> [!IMPORTANT]
> Discord en Discord.Net zijn allebei continue bezig met ontwikkelen, waardoor de applicatie zelf niet meer actueel en up-to-date is. Wel zijn de C# regels die ik heb geschreven een goed en representatief voorbeeld van hoe ik mijn discord bots ontwikkel.

## Wat is een discord bot?
[Discord](https://discord.com/) is een online messaging platform dat developers de mogelijkheid geeft om geautomatiseerde 'bot' accounts te beheren en te ontwikkelen. Hiermee kunnen gebruikers allerlei commando's aanroepen om gebruik te maken van verschillende functionaliteiten.

### Ping commando.
![image](https://github.com/HugoVerweij/DiscordBot/assets/163334632/cd160e92-5379-4aad-a4e7-23e687cbc6eb)  
Hierboven is bijvoorbeeld een ping commando te vinden. De gebruiker kan precies zien wat de heartbeat is van de bot naar de server.  

## Music component voorbeelden
Hier een paar voorbeelden vanuit het music component, een van de command modules die de gebruikers aan kunnen roepen.
https://github.com/HugoVerweij/DiscordBot/blob/a87b939ceef3be1e49196f4cabc08e28d458f0fc/Commands/Clusters/MusicModule.cs#L32

### Play
![Discord_4nclwIB1Vy](https://github.com/HugoVerweij/DiscordBot/assets/163334632/24e65353-77a7-40b1-b633-3fe859ca7f3c)  
Dit is de play commando van het music component. Hier kan de gebruiker een link, search term of playlist gebruiken om af te spelen.

### Search
![Discord_E1sNwZbZkc](https://github.com/HugoVerweij/DiscordBot/assets/163334632/1b8494d1-e9ec-45e1-bc8e-c7477f4aa09d)  
Dit is de play commando van het music component. Bij dit voorbeeld wordt een zoekterm gebruikt inplaats van een directe link.

### Queue
![Discord_fr512TayQX](https://github.com/HugoVerweij/DiscordBot/assets/163334632/0e87f2a6-27d1-43d4-a6d9-24a5c0432b9e)  
Dit is de queue commando van het music component. Hier kan de gebruiker zien welke nummers op de afspeel lijst staan.  
