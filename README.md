# SearchNow
Multi-purpose windows search bar developed with wpf .NET 4.5.

I am still testing this software.
There's some serious cleanup to be done to the source so bear with me.

---

#Usage

+ Hot Keys

	`Ctrl+Alt+F` - open/focus/close search box

+ Queries

	|Input                 | Effect                                            |
	| -----------------    | :-----------------------------------------------: |
	| `plaintext`          | Search with the default engine                    |
	| `?e=shortcut:query;` | Search for 'query' with the specified engine      |
	| `?d=shortcut;`       | Set engine with 'shortcut' as default             |
	| `r=myfile.xml;`      | Load search engine definitions from 'myfile.xml'  |
	| `c=x;`               | Exit                                              |

---

#Examples:
+ `myquerytosearch` - Search for 'myquerytosearch' using the default engine. (plaintext search)
+ `?e=y:rickroll;` - Search for 'rickroll' using the search engine that has 'y' as shortcut.
+ `?d=y;` - Set the search engine that has 'y' as shortcut, as default.
+ `?r=mydefinitions.xml` - Load definitions for 'mydefinitions.xml' file.



>NOTE:
>
>All of the above options are mixable.For example:
>
> `?r=engines.xml;?d=g;?e=y:whatever;plaintext`

>This will load definitions from 'engines.xml', set engine with 'g' as shortcut default,
>search for 'whatever' using the engine with 'y' as shortcut, and then use the default engine
>('g' that we just set) to search for 'plaintext'
 
 ---
 
#Engine definitions

Example definitions file:
(I think this is pretty much self-explanatory)
```xml
<EngineDescriptions>
	<Engine>
		<Name>Youtube</Name>
		<Shortcut>y</Shortcut>
		<Query>https://www.youtube.com/results?search_query={0}</Query>
	</Engine>
	<Engine>
		<Name>Google</Name>
		<Shortcut>g</Shortcut>
		<Query>https://www.google.gr/#q={0}</Query>
	</Engine>
	<Engine>
		<Name>Windows Explorer</Name>
		<Shortcut>ws</Shortcut>
		<Query>search-ms:query={0}&amp;</Query>
	</Engine>
	<Engine>
		<Name>Wikipedia</Name>
		<Shortcut>w</Shortcut>
		<Query>https://en.wikipedia.org/w/index.php?search={0}</Query>
	</Engine>
</EngineDescriptions>
```
