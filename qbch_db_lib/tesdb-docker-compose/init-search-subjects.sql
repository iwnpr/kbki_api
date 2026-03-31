-- 2. Создание схемы
CREATE SCHEMA qbch;

-- 3. Создание функции поиска субъектов
CREATE OR REPLACE FUNCTION qbch.fip_search_all_subjects_v2(request xml DEFAULT NULL::xml)
 RETURNS bigint[]
 LANGUAGE plpgsql
AS $function$
	DECLARE		
	BEGIN		
		IF (xpath('ЗапросСведенийОПлатежах/Запрос/Субъект/ДокументЛичности/Серия/text()', request))[1]::TEXT = '0000'		
		THEN
			RETURN ARRAY[1,2,3];		
		ELSIF (xpath('ЗапросСведенийОПлатежах/Запрос/Субъект/ДокументЛичности/Серия/text()', request))[1]::TEXT = '0009'		
		THEN		
			RETURN ARRAY[5];		
		ELSE RETURN NULL; 		
		END IF;
	END;
$function$
;