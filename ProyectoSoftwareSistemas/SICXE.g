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
    : OPCODE_F1 f3Operands?
    | OPCODE_F1 value?
    ;

f2
    : f2_oneReg
    | f2_twoReg
    | f2_regNum
    | f2_num
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

    f2_oneReg
    : OPCODE_F2_1REG REG
    ;

f2_twoReg
    : OPCODE_F2_2REG REG COMMA REG
    ;

f2_regNum
    : OPCODE_F2_REGNUM REG COMMA NUMBER
    ;

f2_num
    : OPCODE_F2_NUM NUMBER
    ;

simpleOperand
    : expr
    ;

indexedOperand
    : expr COMMA REG
    ;

immediateOperand
    : HASH expr
    ;

indirectOperand
    : AT expr
    ;

/* DIRECTIVAS */

directive
    : DIRECTIVE expr?
    ;

/* VALORES */

expr
    : expr PLUS term
    | expr MINUS term
    | term
    ;

term
    : term MUL factor
    | term DIV factor
    | factor
    ;

factor
    : value
    | MINUS factor
    | LPAREN expr RPAREN
    ;

value
    : ID
    | NUMBER
    | HEX
    | CHAR_LITERAL
    | HEX_LITERAL
    | MUL
    ;

/* TOKENS AUXILIARES */

PLUS  : '+' ;
COMMA : ',' ;
HASH  : '#' ;
AT    : '@' ;
MINUS  : '-' ;
MUL    : '*' ;
DIV   : '/' ;
LPAREN : '(' ;
RPAREN : ')' ;

/* OPCODES POR FORMATO */

OPCODE_F1
    : 'FIX' | 'FLOAT' | 'HIO' | 'NORM' | 'SIO' | 'TIO'
    ;

OPCODE_F2_1REG
    : 'CLEAR'
    | 'TIXR'
    ;

OPCODE_F2_2REG
    : 'ADDR'
    | 'COMPR'
    | 'DIVR'
    | 'MULR'
    | 'RMO'
    | 'SUBR'
    ;

OPCODE_F2_REGNUM
    : 'SHIFTL'
    | 'SHIFTR'
    ;

OPCODE_F2_NUM
    : 'SVC'
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
    | 'BYTE' | 'BASE' | 'NOBASE' | 'EQU' | 'ORG'
    ;

/* REGISTROS */

REG
    : 'A' | 'X' | 'L' | 'B' | 'S' | 'T' | 'F' | 'PC' | 'SW'
    ;

/* TOKENS BÁSICOS */
HEX_LITERAL
    : 'X\'' [0-9A-F]+ '\''
    ;
CHAR_LITERAL
    : 'C\'' (~['\r\n'])* '\''
    ;
ID
    : [A-Z][A-Z0-9]*
    ;

NUMBER
    : [0-9]+
    ;

HEX
    : [0-9A-F]+ 'H'
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
