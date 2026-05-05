-- DROP SCHEMA qbch;

CREATE SCHEMA qbch;

-- DROP SEQUENCE qbch.td_permissions_key_id_seq;

CREATE SEQUENCE qbch.td_permissions_key_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.td_permissions_key_id_seq1;

CREATE SEQUENCE qbch.td_permissions_key_id_seq1
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.td_users_individual_key_id_seq;

CREATE SEQUENCE qbch.td_users_individual_key_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.td_users_individual_key_id_seq1;

CREATE SEQUENCE qbch.td_users_individual_key_id_seq1
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.td_users_individual_key_id_seq2;

CREATE SEQUENCE qbch.td_users_individual_key_id_seq2
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.td_users_legal_key_id_seq;

CREATE SEQUENCE qbch.td_users_legal_key_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.td_users_legal_key_id_seq1;

CREATE SEQUENCE qbch.td_users_legal_key_id_seq1
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.td_users_legal_key_id_seq2;

CREATE SEQUENCE qbch.td_users_legal_key_id_seq2
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.te_cert_manage_key_id_seq;

CREATE SEQUENCE qbch.te_cert_manage_key_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.te_dlanswers_key_id_seq;

CREATE SEQUENCE qbch.te_dlanswers_key_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.te_dlanswers_key_id_seq1;

CREATE SEQUENCE qbch.te_dlanswers_key_id_seq1
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.te_dlanswers_key_id_seq2;

CREATE SEQUENCE qbch.te_dlanswers_key_id_seq2
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.te_dlputanswers_key_id_seq;

CREATE SEQUENCE qbch.te_dlputanswers_key_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.te_dlputanswers_key_id_seq1;

CREATE SEQUENCE qbch.te_dlputanswers_key_id_seq1
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.te_dlputanswers_key_id_seq2;

CREATE SEQUENCE qbch.te_dlputanswers_key_id_seq2
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.te_dlputs_key_id_seq;

CREATE SEQUENCE qbch.te_dlputs_key_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.te_dlputs_key_id_seq1;

CREATE SEQUENCE qbch.te_dlputs_key_id_seq1
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.te_dlputs_key_id_seq2;

CREATE SEQUENCE qbch.te_dlputs_key_id_seq2
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.te_dlrequests_key_id_seq;

CREATE SEQUENCE qbch.te_dlrequests_key_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.te_dlrequests_key_id_seq1;

CREATE SEQUENCE qbch.te_dlrequests_key_id_seq1
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.te_dlrequests_key_id_seq2;

CREATE SEQUENCE qbch.te_dlrequests_key_id_seq2
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.te_qbch_dlrequests_key_id_seq;

CREATE SEQUENCE qbch.te_qbch_dlrequests_key_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.te_qbch_dlrequests_key_id_seq1;

CREATE SEQUENCE qbch.te_qbch_dlrequests_key_id_seq1
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.te_qbch_dlrequests_key_id_seq2;

CREATE SEQUENCE qbch.te_qbch_dlrequests_key_id_seq2
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.te_subjects_documents_key_id_seq;

CREATE SEQUENCE qbch.te_subjects_documents_key_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.te_subjects_documents_key_id_seq1;

CREATE SEQUENCE qbch.te_subjects_documents_key_id_seq1
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.te_subjects_documents_key_id_seq2;

CREATE SEQUENCE qbch.te_subjects_documents_key_id_seq2
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.te_subjects_full_name_key_id_seq;

CREATE SEQUENCE qbch.te_subjects_full_name_key_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.te_subjects_full_name_key_id_seq1;

CREATE SEQUENCE qbch.te_subjects_full_name_key_id_seq1
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.te_subjects_full_name_key_id_seq2;

CREATE SEQUENCE qbch.te_subjects_full_name_key_id_seq2
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.te_subjects_key_id_seq;

CREATE SEQUENCE qbch.te_subjects_key_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.te_subjects_key_id_seq1;

CREATE SEQUENCE qbch.te_subjects_key_id_seq1
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.te_subjects_key_id_seq2;

CREATE SEQUENCE qbch.te_subjects_key_id_seq2
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.tr_abonent_certificates_key_id_seq;

CREATE SEQUENCE qbch.tr_abonent_certificates_key_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.tr_abonent_certificates_key_id_seq1;

CREATE SEQUENCE qbch.tr_abonent_certificates_key_id_seq1
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.tr_abonents_key_id_seq;

CREATE SEQUENCE qbch.tr_abonents_key_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.tr_abonents_key_id_seq1;

CREATE SEQUENCE qbch.tr_abonents_key_id_seq1
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.tr_dlrequest_types_key_id_seq;

CREATE SEQUENCE qbch.tr_dlrequest_types_key_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.tr_dlrequest_types_key_id_seq1;

CREATE SEQUENCE qbch.tr_dlrequest_types_key_id_seq1
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.tr_error_codes_key_id_seq;

CREATE SEQUENCE qbch.tr_error_codes_key_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.tr_error_codes_key_id_seq1;

CREATE SEQUENCE qbch.tr_error_codes_key_id_seq1
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.tr_qbch_response_types_key_id_seq;

CREATE SEQUENCE qbch.tr_qbch_response_types_key_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.tr_services_key_id_seq;

CREATE SEQUENCE qbch.tr_services_key_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.tr_services_key_id_seq1;

CREATE SEQUENCE qbch.tr_services_key_id_seq1
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.tr_user_types_key_id_seq;

CREATE SEQUENCE qbch.tr_user_types_key_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE qbch.tr_user_types_key_id_seq1;

CREATE SEQUENCE qbch.tr_user_types_key_id_seq1
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;-- qbch.td_users_legal definition

-- Drop table

-- DROP TABLE qbch.td_users_legal;

CREATE TABLE qbch.td_users_legal (
	key_id bigserial NOT NULL,
	is_foreign bool NULL,
	inn text NULL,
	ogrn text NULL,
	full_name text NULL,
	short_name text NULL,
	other_name text NULL,
	CONSTRAINT td_users_legal_pkey PRIMARY KEY (key_id)
);


-- qbch.te_dlanswer definition

-- Drop table

-- DROP TABLE qbch.te_dlanswer;

CREATE TABLE qbch.te_dlanswer (
	answer_id uuid NULL,
	request_date timestamp NULL,
	http_status text NULL,
	request_ip text NULL
);


-- qbch.tr_dlrequest_types definition

-- Drop table

-- DROP TABLE qbch.tr_dlrequest_types;

CREATE TABLE qbch.tr_dlrequest_types (
	key_id serial4 NOT NULL,
	description text NOT NULL,
	CONSTRAINT tr_dlrequest_types_pk PRIMARY KEY (key_id)
);


-- qbch.tr_document_types definition

-- Drop table

-- DROP TABLE qbch.tr_document_types;

CREATE TABLE qbch.tr_document_types (
	key_id text NOT NULL,
	doc_description text NOT NULL,
	CONSTRAINT tr_document_types_pk PRIMARY KEY (key_id)
);


-- qbch.tr_error_codes definition

-- Drop table

-- DROP TABLE qbch.tr_error_codes;

CREATE TABLE qbch.tr_error_codes (
	key_id serial4 NOT NULL,
	description text NOT NULL,
	"comments" text NULL,
	CONSTRAINT tr_error_codes_pkey PRIMARY KEY (key_id)
);


-- qbch.tr_qbch_response_types definition

-- Drop table

-- DROP TABLE qbch.tr_qbch_response_types;

CREATE TABLE qbch.tr_qbch_response_types (
	key_id serial4 NOT NULL,
	response_data text NOT NULL,
	whose_data text NOT NULL,
	CONSTRAINT tr_qbch_response_types_pk PRIMARY KEY (key_id)
);


-- qbch.tr_services definition

-- Drop table

-- DROP TABLE qbch.tr_services;

CREATE TABLE qbch.tr_services (
	key_id serial4 NOT NULL,
	service_name text NULL,
	description text NULL,
	CONSTRAINT tr_services_pkey PRIMARY KEY (key_id)
);


-- qbch.tr_user_types definition

-- Drop table

-- DROP TABLE qbch.tr_user_types;

CREATE TABLE qbch.tr_user_types (
	key_id serial4 NOT NULL,
	description text NULL,
	CONSTRAINT tr_user_types_pkey PRIMARY KEY (key_id)
);


-- qbch.td_users_individual definition

-- Drop table

-- DROP TABLE qbch.td_users_individual;

CREATE TABLE qbch.td_users_individual (
	key_id bigserial NOT NULL,
	inn text NULL,
	ogrn text NULL,
	snils text NULL,
	last_name text NOT NULL,
	first_name text NOT NULL,
	middle_name text NULL,
	doc_type_key_id text NOT NULL,
	doc_other_name text NULL,
	doc_seria text NULL,
	doc_number text NOT NULL,
	doc_issue_date date NOT NULL,
	doc_issuer_name text NOT NULL,
	doc_issuer_code text NULL,
	birth_date date NOT NULL,
	birth_place text NOT NULL,
	CONSTRAINT td_users_individual_pkey PRIMARY KEY (key_id),
	CONSTRAINT td_users_individual_tr_doc_types_fkey FOREIGN KEY (doc_type_key_id) REFERENCES qbch.tr_document_types(key_id)
);


-- qbch.tr_abonents definition

-- Drop table

-- DROP TABLE qbch.tr_abonents;

CREATE TABLE qbch.tr_abonents (
	key_id serial4 NOT NULL,
	user_type_id int4 NOT NULL,
	full_name text NOT NULL,
	short_name text NOT NULL,
	inn text NULL,
	ogrn text NOT NULL,
	email text NULL,
	CONSTRAINT tr_abonents_pkey PRIMARY KEY (key_id),
	CONSTRAINT tr_abonents_fk FOREIGN KEY (user_type_id) REFERENCES qbch.tr_user_types(key_id)
);


-- qbch.td_permissions definition

-- Drop table

-- DROP TABLE qbch.td_permissions;

CREATE TABLE qbch.td_permissions (
	key_id serial4 NOT NULL,
	abonents_key_id int4 NOT NULL,
	services_key_id int4 NOT NULL,
	is_granted bool NOT NULL DEFAULT false,
	CONSTRAINT td_permission_pkey PRIMARY KEY (key_id),
	CONSTRAINT td_permissions_tr_abonents_fkey FOREIGN KEY (abonents_key_id) REFERENCES qbch.tr_abonents(key_id),
	CONSTRAINT td_permissions_tr_services_fkey FOREIGN KEY (services_key_id) REFERENCES qbch.tr_services(key_id)
);


-- qbch.te_dlanswers definition

-- Drop table

-- DROP TABLE qbch.te_dlanswers;

CREATE TABLE qbch.te_dlanswers (
	key_id bigserial NOT NULL,
	dlanswer_id text NULL,
	ip_address text NULL,
	request_certificate_thumbprint text NULL,
	abonent_key_id int4 NULL,
	request_date_time timestamp NOT NULL,
	validation_date_time timestamp NULL,
	response_date_time timestamp NOT NULL,
	error_message text NULL,
	error_code_key_id int4 NULL,
	response_xml xml NULL,
	response_signed_data bytea NULL,
	temp_guid text NOT NULL,
	CONSTRAINT te_dlanswers_pk PRIMARY KEY (key_id),
	CONSTRAINT te_dlanswers_tr_abonents_fkey FOREIGN KEY (abonent_key_id) REFERENCES qbch.tr_abonents(key_id),
	CONSTRAINT te_dlanswers_tr_error_code_fkey FOREIGN KEY (error_code_key_id) REFERENCES qbch.tr_error_codes(key_id)
);


-- qbch.te_dlputanswers definition

-- Drop table

-- DROP TABLE qbch.te_dlputanswers;

CREATE TABLE qbch.te_dlputanswers (
	key_id bigserial NOT NULL,
	dlputanswer_id text NULL,
	ip_address text NULL,
	request_certificate_thumbprint text NULL,
	abonent_key_id int4 NULL,
	request_date_time timestamp NOT NULL,
	validation_date_time timestamp NULL,
	response_date_time timestamp NOT NULL,
	error_message text NULL,
	error_code_key_id int4 NULL,
	response_xml xml NULL,
	response_signed_data bytea NULL,
	temp_guid text NOT NULL,
	CONSTRAINT te_dlputanswers_pk PRIMARY KEY (key_id),
	CONSTRAINT te_dlputanswers_tr_abonents_fkey FOREIGN KEY (abonent_key_id) REFERENCES qbch.tr_abonents(key_id),
	CONSTRAINT te_dlputanswers_tr_error_codes_fkey FOREIGN KEY (error_code_key_id) REFERENCES qbch.tr_error_codes(key_id)
);


-- qbch.te_dlputs definition

-- Drop table

-- DROP TABLE qbch.te_dlputs;

CREATE TABLE qbch.te_dlputs (
	key_id bigserial NOT NULL,
	dlputanswer_id text NULL,
	ip_address text NULL,
	request_certificate_thumbprint text NULL,
	abonent_key_id int4 NULL,
	request_date_time timestamp NOT NULL,
	validation_date_time timestamp NULL,
	response_date_time timestamp NULL,
	request_id text NULL,
	request_xml xml NULL,
	request_signed_data bytea NULL,
	error_message text NULL,
	error_code_key_id int4 NULL,
	add_commands_count int4 NOT NULL,
	delete_commands_count int4 NOT NULL,
	response_xml xml NULL,
	response_signed_data bytea NULL,
	CONSTRAINT te_dlrequests_pk_1 PRIMARY KEY (key_id),
	CONSTRAINT te_dlputs_te_abonents_fkey FOREIGN KEY (abonent_key_id) REFERENCES qbch.tr_abonents(key_id),
	CONSTRAINT te_dlputs_te_errors_fkey FOREIGN KEY (error_code_key_id) REFERENCES qbch.tr_error_codes(key_id)
);


-- qbch.te_dlrequests definition

-- Drop table

-- DROP TABLE qbch.te_dlrequests;

CREATE TABLE qbch.te_dlrequests (
	key_id bigserial NOT NULL,
	dlanswer_id text NOT NULL,
	ip_address text NULL,
	request_certificate_thumbprint text NULL,
	abonent_key_id int4 NULL,
	request_date_time timestamp NOT NULL,
	validation_date_time timestamp NULL,
	qbch_total_execution_date_time timestamp NULL,
	response_date_time timestamp NULL,
	request_id text NULL,
	requset_type_key_id int4 NULL,
	user_type_id int4 NULL,
	user_id int8 NULL,
	request_xml xml NULL,
	request_signed_data bytea NULL,
	error_message text NULL,
	error_code_key_id int4 NULL,
	response_xml xml NULL,
	response_signed_data bytea NULL,
	inserted timestamp NULL DEFAULT now(),
	CONSTRAINT te_dlrequests_pk PRIMARY KEY (key_id),
	CONSTRAINT te_dlrequests_tr_abonents_fkey FOREIGN KEY (abonent_key_id) REFERENCES qbch.tr_abonents(key_id),
	CONSTRAINT te_dlrequests_tr_errors_fkey FOREIGN KEY (error_code_key_id) REFERENCES qbch.tr_error_codes(key_id),
	CONSTRAINT te_dlrequests_tr_request_types_fkey FOREIGN KEY (requset_type_key_id) REFERENCES qbch.tr_dlrequest_types(key_id),
	CONSTRAINT te_dlrequests_user_types_fkey FOREIGN KEY (user_type_id) REFERENCES qbch.tr_user_types(key_id)
);


-- qbch.te_qbch_dlrequests definition

-- Drop table

-- DROP TABLE qbch.te_qbch_dlrequests;

CREATE TABLE qbch.te_qbch_dlrequests (
	key_id bigserial NOT NULL,
	dlrequest_main_key_id int8 NOT NULL,
	qbch_key_id int4 NOT NULL,
	task_start_date_time timestamp NOT NULL,
	dlrequest_start_date_time timestamp NULL,
	dlanswer_start_date_time timestamp NULL,
	response_date_time timestamp NULL,
	request_xml xml NULL,
	request_signed_data bytea NULL,
	error_message text NULL,
	error_code_key_id int4 NULL,
	response_id text NULL,
	dlrequest_resend_count int4 NOT NULL,
	dlanswer_resend_count int4 NOT NULL,
	response_type int4 NULL,
	response_xml xml NULL,
	response_signed_data bytea NULL,
	CONSTRAINT te_qbch_dlrequests_pk PRIMARY KEY (key_id),
	CONSTRAINT te_qbch_abonent_fkey FOREIGN KEY (qbch_key_id) REFERENCES qbch.tr_abonents(key_id),
	CONSTRAINT te_qbch_dlrequests_fkey FOREIGN KEY (dlrequest_main_key_id) REFERENCES qbch.te_dlrequests(key_id),
	CONSTRAINT te_qbch_dlrequests_tr_error_codes_fkey FOREIGN KEY (error_code_key_id) REFERENCES qbch.tr_error_codes(key_id),
	CONSTRAINT te_qbch_dlrequests_tr_response_types_fkey FOREIGN KEY (response_type) REFERENCES qbch.tr_qbch_response_types(key_id)
);


-- qbch.te_subjects definition

-- Drop table

-- DROP TABLE qbch.te_subjects;

CREATE TABLE qbch.te_subjects (
	key_id bigserial NOT NULL,
	request_key_id int8 NOT NULL,
	birth_day date NULL,
	inn text NULL,
	psrn text NULL,
	snils text NULL,
	CONSTRAINT te_subjects_pkey PRIMARY KEY (key_id),
	CONSTRAINT te_subjects_te_requests_fkey FOREIGN KEY (request_key_id) REFERENCES qbch.te_dlrequests(key_id)
);


-- qbch.te_subjects_documents definition

-- Drop table

-- DROP TABLE qbch.te_subjects_documents;

CREATE TABLE qbch.te_subjects_documents (
	key_id bigserial NOT NULL,
	subject_key_id int8 NOT NULL,
	doc_type_key_id text NOT NULL,
	doc_series text NULL,
	doc_number text NOT NULL,
	doc_date_issue date NOT NULL,
	country_code int4 NULL,
	CONSTRAINT te_subjects_documents_pkey PRIMARY KEY (key_id),
	CONSTRAINT te_subjects_documents_fkey FOREIGN KEY (doc_type_key_id) REFERENCES qbch.tr_document_types(key_id),
	CONSTRAINT te_subjects_documents_te_subjects_fkey FOREIGN KEY (subject_key_id) REFERENCES qbch.te_subjects(key_id)
);


-- qbch.te_subjects_full_name definition

-- Drop table

-- DROP TABLE qbch.te_subjects_full_name;

CREATE TABLE qbch.te_subjects_full_name (
	key_id bigserial NOT NULL,
	subject_key_id int8 NOT NULL,
	last_name text NULL,
	first_name text NULL,
	middle_name text NULL,
	CONSTRAINT te_subjects_full_name_pkey PRIMARY KEY (key_id),
	CONSTRAINT te_subjects_full_name_te_subjects_fkey FOREIGN KEY (subject_key_id) REFERENCES qbch.te_subjects(key_id)
);


-- qbch.tr_abonent_certificates definition

-- Drop table

-- DROP TABLE qbch.tr_abonent_certificates;

CREATE TABLE qbch.tr_abonent_certificates (
	key_id serial4 NOT NULL,
	abonent_key_id int4 NOT NULL,
	thumbprint text NOT NULL,
	expiration_date timestamp NOT NULL,
	is_active bool NULL DEFAULT false,
	CONSTRAINT tr_abonent_certificates_pkey PRIMARY KEY (key_id),
	CONSTRAINT tr_abonent_certificates_tr_abonents_fkey FOREIGN KEY (abonent_key_id) REFERENCES qbch.tr_abonents(key_id)
);


-- qbch.te_cert_manage definition

-- Drop table

-- DROP TABLE qbch.te_cert_manage;

CREATE TABLE qbch.te_cert_manage (
	key_id bigserial NOT NULL,
	service_type_id int4 NOT NULL,
	ip_address text NULL,
	request_certificate_thumbprint text NULL,
	abonent_key_id int4 NULL,
	request_date_time timestamp NULL,
	validation_date_time timestamp NULL,
	response_date_time timestamp NULL,
	certificate_key_id int4 NULL,
	request_id text NULL,
	cert_data bytea NULL,
	sign_data bytea NULL,
	error_message text NULL,
	error_code_key_id int4 NULL,
	response_xml xml NULL,
	response_signed_data bytea NULL,
	temp_guid text NULL,
	inserted timestamp NOT NULL DEFAULT now(),
	CONSTRAINT te_cert_manage_pk PRIMARY KEY (key_id),
	CONSTRAINT abonent_key_id_fk FOREIGN KEY (abonent_key_id) REFERENCES qbch.tr_abonents(key_id),
	CONSTRAINT service_key_id_fk FOREIGN KEY (service_type_id) REFERENCES qbch.tr_services(key_id),
	CONSTRAINT te_cert_manage_fk FOREIGN KEY (certificate_key_id) REFERENCES qbch.tr_abonent_certificates(key_id)
);



--CREATE OR REPLACE FUNCTION qbch.fex_info_of_prohibition(subj_id bigint[])
-- RETURNS xml
-- LANGUAGE plpgsql
--AS $function$
--	DECLARE 
--		respons XML;
--	BEGIN
--			
--		IF subj_id = ARRAY[1::bigint,2::bigint]
--			
--		THEN 
--		
--			respons = XMLELEMENT(name "УсловияЗапрета" 
--						,XMLAGG(cr.prohibition))
--					FROM (SELECT 
--						XMLELEMENT(name "Условие"		
--							,XMLATTRIBUTES("date" AS "ДатаЗаявления"
--								,"time" AS "ВремяЗаявления"
--								,"start" AS "НачалоДействия")
--							,conditions
--						) prohibition
--						FROM (VALUES ('2024-02-01', '10:12:45', '2024-02-02', 5)
--							,('2024-02-01', '10:12:45', '2024-02-02', 1)) 
--						AS t ("date", "time", "start", conditions)) cr;
--		
--								
--								
--		ELSEIF subj_id = ARRAY[3::bigint,4::bigint]
--		
--		THEN 
--		
--			respons = XMLELEMENT(name "СведенийОЗапретеНет");	
--		
--		ELSEIF subj_id = ARRAY[5::bigint,6::bigint]
--		
--		THEN 
--	
--			respons = XMLELEMENT(name "СведенияОЗапретеНеПредоставляются");	
--		
--		END IF;
--	
--		RETURN respons;
--	
--	END;
--$function$
--;

CREATE OR REPLACE FUNCTION qbch.fip_is_permission_granted(data_thumbprint text, data_service_name text)
 RETURNS boolean
 LANGUAGE plpgsql
AS $function$
	BEGIN
		RETURN (SELECT is_granted
				FROM qbch.td_permissions tp
				JOIN qbch.tr_abonent_certificates tac ON tac.abonent_key_id = tp.abonents_key_id 
				JOIN qbch.tr_services ts ON ts.key_id = tp.services_key_id 
				WHERE upper(tac.thumbprint) = upper(data_thumbprint)
					AND ts.service_name = data_service_name);
	END;
$function$
;

CREATE OR REPLACE FUNCTION qbch.get_abonent_key(p_ogrn text)
 RETURNS bigint
 LANGUAGE plpgsql
AS $function$
	BEGIN
		RETURN (SELECT key_id FROM qbch.tr_abonents WHERE ogrn = p_ogrn);
	END;
$function$
;

CREATE OR REPLACE FUNCTION qbch.get_abonent_type(xrequest xml)
 RETURNS integer
 LANGUAGE plpgsql
AS $function$
	DECLARE 
		abonent_name TEXT;
	BEGIN
		abonent_name = (
			SELECT 
				CASE 
					WHEN xpath_exists('/ЗапросСведенийОПлатежах/Абонент/ЮридическоеЛицо', xrequest)
					THEN 'ЮридическоеЛицо'
					WHEN xpath_exists('/ЗапросСведенийОПлатежах/Абонент/ИндивидуальныйПредприниматель', xrequest)
					THEN 'ИндивидуальныйПредприниматель'
					WHEN xpath_exists('/ЗапросСведенийОПлатежах/Абонент/ИностранноеЛицо', xrequest)
					THEN 'ИностранноеЛицо'
				END);
		RETURN (SELECT key_id FROM qbch.tr_subject_types WHERE description = abonent_name);
	END;
$function$
;

CREATE OR REPLACE FUNCTION qbch.get_inn_ogrn_by_thumbprint(p_thumbprint text)
 RETURNS xml
 LANGUAGE plpgsql
AS $function$
	DECLARE
		retxml XML = NULL;
	BEGIN
		IF EXISTS 		
		(
			SELECT *
			FROM qbch.tr_abonent_certificates srt
			JOIN qbch.tr_abonents abn ON abn.key_id = srt.abonent_key_id
			WHERE
				upper(srt.thumbprint) = upper(p_thumbprint)
		)
		THEN
			retxml = 
			(
				SELECT
				XMLELEMENT(name "root",
				CASE
					WHEN length(COALESCE (TRIM(abn.inn), '')) > 0 THEN XMLELEMENT (name "inn", abn.inn)
					ELSE NULL
				END,
				CASE
					WHEN length(COALESCE (TRIM(abn.ogrn), '')) > 0 THEN XMLELEMENT (name "ogrn", abn.ogrn)
					ELSE NULL
				end,
				CASE
					WHEN length(COALESCE (TRIM(abn.full_name), '')) > 0 THEN XMLELEMENT (name "full_name", abn.full_name)
					ELSE NULL
				end,
				CASE
					WHEN length(COALESCE (TRIM(abn.short_name), '')) > 0 THEN XMLELEMENT (name "short_name", abn.short_name)
					ELSE NULL
				END
				)
				FROM qbch.tr_abonent_certificates srt
				JOIN qbch.tr_abonents abn ON abn.key_id = srt.abonent_key_id
				WHERE
					upper(srt.thumbprint) = upper(p_thumbprint)
			);
		END IF;
		RETURN retxml;
	END;
$function$
;


INSERT INTO qbch.tr_user_types (key_id, description) VALUES(1, 'Юридическое лицо');
INSERT INTO qbch.tr_user_types (key_id, description) VALUES(2, 'Индивидуальный предприниматель');
INSERT INTO qbch.tr_user_types (key_id, description) VALUES(3, 'Иностранное лицо');
INSERT INTO qbch.tr_user_types (key_id, description) VALUES(4, 'Иностранное ЮЛ');
INSERT INTO qbch.tr_user_types (key_id, description) VALUES(5, 'Иностранный предприниматель');

INSERT INTO qbch.tr_abonents (key_id, user_type_id, full_name, short_name, inn, ogrn, email) VALUES(2, 1, 'ОБЩЕСТВО НЕОГРАНИЧЕННОЙ ОТВЕТСВЕННОСТИ Деньги вперед', 'ОНО Деньги вперед', '7700770077', '1770001770001', 'money@money.ru');
INSERT INTO qbch.tr_abonents (key_id, user_type_id, full_name, short_name, inn, ogrn, email) VALUES(4, 1, 'ООО НБКИ', 'ООО НБКИ', 'NULL', '1057746710713', 'nbki@money.ru');
INSERT INTO qbch.tr_abonents (key_id, user_type_id, full_name, short_name, inn, ogrn, email) VALUES(6, 1, 'ООО ОКБ', 'ООО ОКБ', 'NULL', '1047796788819', 'okb@money.ru');
INSERT INTO qbch.tr_abonents (key_id, user_type_id, full_name, short_name, inn, ogrn, email) VALUES(3, 1, 'ООО КредитИнфо', 'ООО БКИ КредитИнфо', '7719562097', '1057747734934', 'cinfo@bki-ci.ru');
INSERT INTO qbch.tr_abonents (key_id, user_type_id, full_name, short_name, inn, ogrn, email) VALUES(5, 1, 'ООО СБ', 'ООО СБ', 'NULL', '1247700058319', 'sb@money.ru');
INSERT INTO qbch.tr_abonents (key_id, user_type_id, full_name, short_name, inn, ogrn, email) VALUES(1, 1, 'ОБЩЕСТВО БЕЗ ОТВЕТСВЕННОСТИ Деньги назад', 'ОБО Деньги назад', '7719562097', '1057747734934', 'huaney@money.ru');

INSERT INTO qbch.tr_abonent_certificates (key_id, abonent_key_id, thumbprint, expiration_date, is_active) VALUES(1, 1, '0EFD9D19887B10D9170C487869990B80830A7E05', '2024-12-31 00:00:00.000', true);
INSERT INTO qbch.tr_abonent_certificates (key_id, abonent_key_id, thumbprint, expiration_date, is_active) VALUES(5, 1, '61C3393E139ACADFA907653B8400452D4177B06F', '2024-12-31 00:00:00.000', true);
INSERT INTO qbch.tr_abonent_certificates (key_id, abonent_key_id, thumbprint, expiration_date, is_active) VALUES(6, 1, 'a3c0e9269b17547130cb473ca88fe3a2e65d5d6c', '2024-12-31 00:00:00.000', true);
INSERT INTO qbch.tr_abonent_certificates (key_id, abonent_key_id, thumbprint, expiration_date, is_active) VALUES(7, 5, 'test', '2024-12-31 00:00:00.000', true);
INSERT INTO qbch.tr_abonent_certificates (key_id, abonent_key_id, thumbprint, expiration_date, is_active) VALUES(8, 5, 'test2', '2025-04-30 00:00:00.000', false);
INSERT INTO qbch.tr_abonent_certificates (key_id, abonent_key_id, thumbprint, expiration_date, is_active) VALUES(17, 3, 'F0C5A2903B25E9AF3EDC7FAAF50C768652CA714E', '2025-02-01 11:27:48.000', false);
INSERT INTO qbch.tr_abonent_certificates (key_id, abonent_key_id, thumbprint, expiration_date, is_active) VALUES(3, 1, '3250421F71D7046F5821E42C6F8F571A4C419AC0', '2024-12-31 00:00:00.000', true);

INSERT INTO qbch.tr_services (key_id, service_name, description) VALUES(1, 'dlrequest', 'запрос сведений о среднемесячных платежах Субъекта');
INSERT INTO qbch.tr_services (key_id, service_name, description) VALUES(2, 'dlanswer', 'получение сведений о среднемесячных платежах Субъекта по идентификатору ответа');
INSERT INTO qbch.tr_services (key_id, service_name, description) VALUES(3, 'dlput', 'передача от БКИ данных, необходимых для формирования и предоставления пользователям кредитных историй сведений о среднемесячных платежах Субъекта');
INSERT INTO qbch.tr_services (key_id, service_name, description) VALUES(4, 'dlputanswer', 'получение информации о результатах загрузки данных, необходимых для формирования и предоставления пользователям кредитных историй сведений о среднемесячных платежах Субъекта, в базу данных КБКИ');
INSERT INTO qbch.tr_services (key_id, service_name, description) VALUES(5, 'certadd', 'добавление нового сертификата абонента');
INSERT INTO qbch.tr_services (key_id, service_name, description) VALUES(6, 'certrevoke', 'отзыв сертификата абонента');


INSERT INTO qbch.td_permissions (key_id, abonents_key_id, services_key_id, is_granted) VALUES(4, 2, 3, true);
INSERT INTO qbch.td_permissions (key_id, abonents_key_id, services_key_id, is_granted) VALUES(2, 1, 3, true);
INSERT INTO qbch.td_permissions (key_id, abonents_key_id, services_key_id, is_granted) VALUES(3, 2, 1, true);
INSERT INTO qbch.td_permissions (key_id, abonents_key_id, services_key_id, is_granted) VALUES(1, 1, 1, true);
INSERT INTO qbch.td_permissions (key_id, abonents_key_id, services_key_id, is_granted) VALUES(5, 5, 1, true);
INSERT INTO qbch.td_permissions (key_id, abonents_key_id, services_key_id, is_granted) VALUES(6, 1, 5, true);
INSERT INTO qbch.td_permissions (key_id, abonents_key_id, services_key_id, is_granted) VALUES(7, 1, 6, true);

INSERT INTO qbch.tr_dlrequest_types (key_id, description) VALUES(1, 'Не в режиме одного окна');
INSERT INTO qbch.tr_dlrequest_types (key_id, description) VALUES(2, 'В режиме одного окна');

INSERT INTO qbch.tr_document_types (key_id, doc_description) VALUES('21', 'Паспорт гражданина Российской Федерации');
INSERT INTO qbch.tr_document_types (key_id, doc_description) VALUES('22.1', 'Паспорт гражданина Российской Федерации, удостоверяющий его личность за пределами территории Российской Федерации');
INSERT INTO qbch.tr_document_types (key_id, doc_description) VALUES('22.3', 'Служебный паспорт, удостоверяющий личность гражданина Российской Федерации за пределами территории Российской Федерации');
INSERT INTO qbch.tr_document_types (key_id, doc_description) VALUES('23', 'Удостоверение личности моряка');
INSERT INTO qbch.tr_document_types (key_id, doc_description) VALUES('24', 'Удостоверение личности военнослужащего');
INSERT INTO qbch.tr_document_types (key_id, doc_description) VALUES('25', 'Военный билет военнослужащего');
INSERT INTO qbch.tr_document_types (key_id, doc_description) VALUES('26', 'Временное удостоверение личности гражданина Российской Федерации, выдаваемое на период оформления паспорта гражданина Российской Федерации');
INSERT INTO qbch.tr_document_types (key_id, doc_description) VALUES('27', 'Свидетельство о рождении гражданина Российской Федерации');
INSERT INTO qbch.tr_document_types (key_id, doc_description) VALUES('28', 'Иной документ, удостоверяющий личность гражданина Российской Федерации в соответствии с законодательством Российской Федерации');
INSERT INTO qbch.tr_document_types (key_id, doc_description) VALUES('31', 'Паспорт иностранного гражданина либо иной документ, установленный федеральным законом или признаваемый в соответствии с международным договором Российской Федерации в качестве документа, удостоверяющего личность иностранного гражданина');
INSERT INTO qbch.tr_document_types (key_id, doc_description) VALUES('32', 'Документ, выданный иностранным государством и признаваемый в соответствии с международным договором Российской Федерации в качестве документа, удостоверяющего личность лица без гражданства');
INSERT INTO qbch.tr_document_types (key_id, doc_description) VALUES('35', 'Иной документ, признаваемый документом, удостоверяющим личность лица без гражданства в соответствии с законодательством Российской Федерации и международным договором Российской Федерации');
INSERT INTO qbch.tr_document_types (key_id, doc_description) VALUES('37', 'Удостоверение беженца');
INSERT INTO qbch.tr_document_types (key_id, doc_description) VALUES('38', 'Удостоверение вынужденного переселенца');
INSERT INTO qbch.tr_document_types (key_id, doc_description) VALUES('999', 'Иной документ');
INSERT INTO qbch.tr_document_types (key_id, doc_description) VALUES('22.2', 'Дипломатический паспорт');

INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(1, 'Метод передачи запроса не соответствует ожидаемому', 'Метод передачи запроса не соответствует требуемому настоящим Порядком методу, например, запрос /dlanswer передан методом POST вместо GET');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(2, 'Запрос не содержит данных', 'Запрос не содержит файла запроса (тело запроса не заполнено/не передано)');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(3, 'Запрос не содержит обязательных параметров', 'Запрос не содержит параметров, которые в соответствии с настоящим Порядком должны быть переданы, например, запрос /certadd не содержит параметра id. В описании ошибки должны быть указаны отсутствующие параметры');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(4, 'УЭП некорректна', 'Не удалось открепить и проверить УЭП файла запроса; УЭП передана в некорректном формате; УЭП отсутствует');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(5, 'Истек строк сертификата УЭП', 'Срок сертификата переданной УЭП истек');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(6, 'УЭП не соответствует абоненту', 'Реквизиты сертификата УЭП не соответствуют реквизитам сертификата, посредством которого установлено соединение. В описании ошибки должны быть указаны реквизиты УЭП и ожидаемые реквизиты');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(7, 'Некорректный формат запроса', 'Полученный в запросе файл не идентифицируется как криптографическое сообщение в формате PKCS#7, содержащее запрос и УЭП');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(8, 'Неподдерживаемая кодировка', 'Кодировка, указанная в XML файле запроса, не поддерживается сервером. В описании ошибки должно быть продублировано название неподдерживаемой кодировки');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(9, 'Запрос не соответствует схеме', 'Запрос не соответствует XSD-схеме запроса. В описании ошибки должна быть включена информация о том, почему запрос не соответствует схеме');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(10, 'Реквизиты запроса не соответствуют абоненту', 'Реквизиты абонента, указанные в запросе (в блоке «Абонент» для /dlrequest или в блоке «БКИ» для /dlput), не соответствуют реквизитам сертификата, посредством которого установлено соединение. В описании ошибки должны быть указаны реквизиты запроса и ожидаемые реквизиты');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(11, 'Идентификатор запроса не уникален', 'Идентификатор запроса ранее передавался данным абонентом в составе другого запроса такого же типа');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(12, 'Ответ не готов', 'Подготовка ответа по указанному идентификатору не закончена. Необходимо повторить запрос позднее');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(13, 'Отсутствует действующее согласие Субъекта', 'Указанная дата выдачи согласия плюс срок действия согласия более ранняя, чем текущая дата; реквизиты лица, указанные в блоке «Выдано», не соответствуют реквизитам лица, указанным в блоке «Источник»; одна или несколько целей, указанных в блоке «Запрос» отсутствует, в блоке «Согласие» и др. Описание ошибки должно содержать конкретную причину');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(14, 'Взаимодействие с абонентом в режиме «одно окно» не предусмотрено договором', 'В атрибуте «ТипЗапроса» указано значение «2», в то время как договором с абонентом не предусмотрено взаимодействие в режиме «одно окно»');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(15, 'Запрос содержит некорректные данные', 'Запрос содержит ошибочные данные, не выявляющиеся XSD схемой запроса, например, дата выдачи ДУЛ более ранняя, чем дата рождения. Описание ошибки должно включать конкретную причину возникновения ошибки');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(16, 'Указан некорректный идентификатор ответа', 'В качестве значения параметра id запроса /dlanswer или /dlputanswer указан идентификатор, который не выдавался абоненту');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(17, 'Не удалось установить соединение', 'При подготовке ответа на запрос в режиме «одно окно» не удалось установить соединение с другим КБКИ. Данная ошибка возвращается только в составе сведений о среднемесячных платежах Субъекта внутри одного или нескольких блоков «КБКИ»');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(18, 'Время ожидания ответа истекло', 'При подготовке ответа на запрос типа «одно окно» другое КБКИ по истечении максимального времени ожидания ответа возвращает квитанцию с ошибкой «Ответ не готов». Данная ошибка возвращается только в составе сведений о среднемесячных платежах Субъекта внутри одного или нескольких блоков «КБКИ»');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(19, 'Ответ не соответствует схеме', 'При подготовке ответа на запрос типа «одно окно» другое КБКИ вернуло несоответствующий схеме qcb_answer.xsd (Приложение 2) ответ. Данная ошибка возвращается только в составе сведений о среднемесячных платежах Субъекта внутри одного или нескольких блоков «КБКИ»');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(20, 'Договор с указанным УИД не найден', 'В запросе /dlput в атрибуте «УИД» для договора с операцией «Удалить» указан УИД, информация о котором ранее не передавалась абонентом');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(21, 'Сведения о величине среднемесячного платежа по договору и дате его расчета не найдены', 'В запросе /dlput в атрибуте «ДатаРасчета» блока «Удалить» указана дата, на которую ранее не передавалась информация о величине среднемесячного платежа по договору');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(22, 'Запрос не доступен для абонента', 'Например, запрос /dlput или /dlputanswer передан абонентом, не являющимся БКИ, либо запрос /dlrequest или /dlanswer передан БКИ, не являющимся квалифицированным');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(23, 'Дата запроса указана некорректно', 'В атрибуте «Дата» блока «Запрос» запроса /dlrequest указана дата, не являющаяся текущей');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(24, 'Ошибка при проверке УКЭП', 'Не удалось проверить валидность УКЭП (например, из-за временной недоступности удостоверяющего центра, текст ошибки должен быть дополнен конкретной причиной ее возникновения)');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(25, 'Сведения о запрете не могут быть предоставлены', 'Сведения о запрете (снятии запрета) на заключение договоров потребительского займа (кредита) не могут быть предоставлены в связи с отсутствием информации об ИНН субъекта и (или) результатах проверки ИНН, либо атрибут «ПризнакПроверки» содержит код «0»');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(26, 'Некорректное количество блоков', 'Количество блоков «Запрос» не соответствует режиму запроса или порядковые номера запросов заполнены некорректно');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(27, 'Отсутствует согласие субъекта', 'В запросе на получение сведений о среднемесячных платежах или сведений для предупреждения мошенничества отсутствует блок «Согласие»');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(28, 'В ответе КБКИ отсутствуют запрошенные сведения', 'Формируется КБКИ-контрагентом в случаях, когда в ответе на пакетный запрос отсутствует часть запрошенных данных');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(29, 'Субъект не найден', 'В запросе /dlput в блоке «Договор» или «ОбращениеОбязательство» с вложенным блоком «Удалить» указаны сведения о Субъекте, информация о котором ранее не передавалась абонентом');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(30, 'Обращение (обязательство) с указанным УИД не найдено', 'В запросе /dlput в атрибуте «УИД» для обращения (обязательства) с операцией «Удалить» указан УИД обращения (договора (сделки)), информация о котором ранее не передавалась абонентом для указанного Субъекта');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(31, 'Сведения для предупреждения мошенничества не найдены', 'В запросе /dlput в атрибуте «СтадияРассмотрения» для обращения (обязательства) с операцией «Удалить» указана стадия, информация о которой ранее не передавалась абонентом для указанного обращения (обязательства)');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(99, 'Другая ошибка', 'Другая ошибка, не предусмотренная данной таблицей. В описании ошибки должна быть дополнительно описана причина ее возникновения');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(0, 'Нет ошибок', 'Запрос успешно обработан');
INSERT INTO qbch.tr_error_codes (key_id, description, "comments") VALUES(500, 'Ошибка на стороне КБКИ', '');

INSERT INTO qbch.tr_qbch_response_types (key_id, response_data, whose_data) VALUES(1, 'Субъект не найден', '-');
INSERT INTO qbch.tr_qbch_response_types (key_id, response_data, whose_data) VALUES(2, 'Обязательств нет', '-');
INSERT INTO qbch.tr_qbch_response_types (key_id, response_data, whose_data) VALUES(3, 'Результативный ответ', 'Только наши данные');
INSERT INTO qbch.tr_qbch_response_types (key_id, response_data, whose_data) VALUES(4, 'Результативный ответ', 'Наши данные + Данные другого бюро');
INSERT INTO qbch.tr_qbch_response_types (key_id, response_data, whose_data) VALUES(5, 'Результативный ответ', 'Данные только другого бюро');
INSERT INTO qbch.tr_qbch_response_types (key_id, response_data, whose_data) VALUES(7, 'Ошибка получения ответа', 'Не удалось получить данные от другого КБКИ');
INSERT INTO qbch.tr_qbch_response_types (key_id, response_data, whose_data) VALUES(6, 'Ошибка в ответе КБКИ', 'КБКИ вернул ошибку');
