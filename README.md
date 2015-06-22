# Calculator
Current project. Graphing Calculator. C#

Day 1: spent probably 5-6 hours thinking of the design and creating some basic classes.

Day 2: spent 4~ hours attempting to write the default equation parser

Day 3: Finished the basic implementation of the default equation parser, for now.
	   Currently it can:
	    -Contain subexpressions with delimiters, currently just "( ), [ ]", but can easily add more to the arrays
		-Find any function typed as a string within the equation, and evaluate it with the correct number of arguments with a comma as the delimiter
		-Include any number of constants, which can either be simple numbers like '55.7' or subexpressions like B = 'X - 5'
		-Works with any number of spaces between individual entities, or without any spaces anywhere
		-Allows some shorthand notation:
			-"2X" instead of "2 * X"
			-"3(X - 5)" instead of "3 * (X - 5)"
	   Future additions:
		-Allow multiple constants in a row, such as "3ABX", which will eventually be translated to "3*A*B*X" at some point
		-Check for errors during equation parsing and give the appropriate error messages
		-Unit Tests for parsing
		
Day 4: Included a utility class project within this project