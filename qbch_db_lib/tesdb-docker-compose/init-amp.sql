CREATE SCHEMA qbch;

CREATE OR REPLACE FUNCTION qbch.fex_calculation_of_amp(subj_id bigint[])
 RETURNS xml
 LANGUAGE plpgsql
AS $function$
	DECLARE 
		respons XML;
	BEGIN			
		IF subj_id = ARRAY[1::bigint,2::bigint,3::bigint]			
		THEN 		
			respons = XMLELEMENT(name "Обязательства" 
						,XMLAGG(cr.contract))
					FROM (SELECT 
						XMLELEMENT(name "БКИ"		
							,XMLATTRIBUTES('1234567891234'  AS "ОГРН")
							,XMLAGG(XMLELEMENT(name "Договор"
								,XMLATTRIBUTES('b72bf2fa-5c4a-11ed-bba0-e44a92e98d7e-c' AS "УИД")
								,XMLELEMENT(name "СреднемесячныйПлатеж"
									,XMLATTRIBUTES('RUB' AS "Валюта"
										,'2024-01-05' AS "ДатаРасчета")
									,500)))) contract) cr;								
								
		ELSE respons = XMLELEMENT(name "ОбязательствНет");			
		END IF;	
		RETURN respons;	
	END;
$function$;