all: report.pdf

%.pdf: %.tex
	latexmk -pv -quiet -xelatex $<

clean:
	latexmk -c -quiet

clobber:
	latexmk -CA -quiet

.PHONY: all clean clobber
