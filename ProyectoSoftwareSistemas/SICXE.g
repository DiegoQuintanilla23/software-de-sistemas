grammar SICXE;

options {
    language = CSharp;
}

/* REGLAS SINTÁCTICAS */

program
    : line* EOF
    ;

line
    : label? statement NEWLINE
    | NEWLINE
    ;

statement
    : extendedInstr
    | instruction
    | directive
    ;

label
    : ID
    ;

/* INSTRUCCIONES */

instruction
    : f1
    | f2
    | f3
    ;

extendedInstr
    : PLUS f3
    ;

/* FORMATOS */

f1
    : OPCODE_F1
    ;

f2
    : OPCODE_F2 REG
    | OPCODE_F2 REG COMMA REG
    | OPCODE_F2 REG COMMA NUMBER
    ;

f3
    : OPCODE_F3 f3Operands?
    ;

/* OPERANDOS FORMATO 3 / 4 */

f3Operands
    : simpleOperand
    | indexedOperand
    | immediateOperand
    | indirectOperand
    ;

simpleOperand
    : value
    ;

indexedOperand
    : value COMMA REG
    ;

immediateOperand
    : HASH value
    ;

indirectOperand
    : AT value
    ;

/* DIRECTIVAS */

directive
    : DIRECTIVE value?
    ;

/* VALORES */

value
    : ID
    | NUMBER
    | HEX
    | CHAR_LITERAL
    ;

/* TOKENS AUXILIARES */

PLUS  : '+' ;
COMMA : ',' ;
HASH  : '#' ;
AT    : '@' ;

/* OPCODES POR FORMATO */

OPCODE_F1
    : 'FIX' | 'FLOAT' | 'HIO' | 'NORM' | 'SIO' | 'TIO'
    ;

OPCODE_F2
    : 'ADDR' | 'CLEAR' | 'COMPR' | 'DIVR' | 'MULR'
    | 'RMO' | 'SHIFTL' | 'SHIFTR' | 'SUBR' | 'SVC' | 'TIXR'
    ;

OPCODE_F3
    : 'ADD' | 'ADDF' | 'AND' | 'COMP' | 'COMPF'
    | 'DIV' | 'DIVF' | 'J' | 'JEQ' | 'JGT' | 'JLT' | 'JSUB'
    | 'LDA' | 'LDB' | 'LDCH' | 'LDF' | 'LDL' | 'LDS'
    | 'LDT' | 'LDX' | 'LPS'
    | 'MUL' | 'MULF' | 'OR'
    | 'RD' | 'RSUB'
    | 'STA' | 'STB' | 'STCH' | 'STF' | 'STI' | 'STL'
    | 'STS' | 'STSW' | 'STT' | 'STX'
    | 'SUB' | 'SUBF'
    | 'TD' | 'TIX' | 'WD'
    ;

/* DIRECTIVAS */

DIRECTIVE
    : 'START' | 'END' | 'WORD' | 'RESW' | 'RESB'
    | 'BYTE' | 'BASE' | 'NOBASE' | 'EQU'
    ;

/* REGISTROS */

REG
    : 'A' | 'X' | 'L' | 'B' | 'S' | 'T' | 'F' | 'PC' | 'SW'
    ;

/* TOKENS BÁSICOS */

ID
    : [A-Z][A-Z0-9]*
    ;

NUMBER
    : [0-9]+
    ;

HEX
    : [0-9A-F]+ 'H'
    ;


CHAR_LITERAL
    : 'C\'' (~['\r\n'])* '\''
    ;

/* COMENTARIOS Y CONTROL */

COMMENT
    : ';' ~[\r\n]* -> skip
    ;


NEWLINE
    : '\r'? '\n'
    ;

WS
    : [ \t]+ -> skip
    ;
